// MIT License
// 
// Copyright (c) 2026 LytharaLab
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OggSifter
{
    public sealed record ExtractedOgg(
        string SourcePath,
        string ExtractedPath,
        long OggOffsetBytes,
        long SourceSizeBytes
    );

    public static class OggMagicExtractor
    {
        // Ogg page capture pattern is "OggS" (ASCII).
        private static readonly byte[] OggS = new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };

        /// <summary>
        /// Extracts any file that contains an "OggS" signature (at offset 0 or later) into outputDir.
        /// If signature is found at a non-zero offset, the extracted file will start from that offset,
        /// which often "repairs" files that have junk bytes before the real OGG stream.
        /// </summary>
        public static async Task<List<ExtractedOgg>> ExtractFromFolderAsync(
            string inputDir,
            string outputDir,
            IProgress<(int scanned, int extracted, string currentFile)>? progress = null,
            CancellationToken ct = default)
        {
            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException($"Input folder not found: {inputDir}");

            Directory.CreateDirectory(outputDir);

            var results = new List<ExtractedOgg>();

            // Use Task.Run to ensure the entire operation runs on a background thread
            return await Task.Run(async () =>
            {
                // Enumerate lazily to avoid loading huge lists into memory.
                int scanned = 0;
                int extracted = 0;
                int lastProgressUpdate = 0;
                const int progressUpdateInterval = 10; // Update UI every 10 files

                foreach (var file in Directory.EnumerateFiles(inputDir, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    scanned++;

                    // Throttle progress updates to avoid UI flooding
                    if (progress != null && (scanned - lastProgressUpdate >= progressUpdateInterval || scanned == 1))
                    {
                        progress.Report((scanned, extracted, file));
                        lastProgressUpdate = scanned;
                    }

                    try
                    {
                        var fi = new FileInfo(file);
                        if (!fi.Exists || fi.Length < 4)
                            continue;

                        // Look for OggS within the first 1 MiB (tunable).
                        // Roblox cache blobs sometimes include prefix junk; we handle that.
                        const int maxProbeBytes = 1024 * 1024;

                        long oggOffset = await FindOggSignatureOffsetAsync(file, maxProbeBytes, ct).ConfigureAwait(false);
                        if (oggOffset < 0)
                            continue;

                        extracted++;
                        string safeName = MakeSafeFileName(fi.Name);
                        string outName = $"{extracted:000000}_{safeName}.ogg";
                        string outPath = Path.Combine(outputDir, outName);

                        await ExtractFromOffsetAsync(file, outPath, oggOffset, ct).ConfigureAwait(false);

                        results.Add(new ExtractedOgg(
                            SourcePath: file,
                            ExtractedPath: outPath,
                            OggOffsetBytes: oggOffset,
                            SourceSizeBytes: fi.Length
                        ));
                    }
                    catch
                    {
                        // Ignore per-file errors; continue scanning.
                    }
                }

                progress?.Report((scanned, extracted, "DONE"));
                return results;
            }, ct).ConfigureAwait(false);
        }

        private static async Task<long> FindOggSignatureOffsetAsync(string filePath, int maxProbeBytes, CancellationToken ct)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 64 * 1024, useAsync: true);

            int toRead = (int)Math.Min(fs.Length, maxProbeBytes);
            if (toRead < 4) return -1;

            byte[] buffer = new byte[toRead];
            int read = 0;
            while (read < toRead)
            {
                ct.ThrowIfCancellationRequested();
                int n = await fs.ReadAsync(buffer.AsMemory(read, toRead - read), ct).ConfigureAwait(false);
                if (n == 0) break;
                read += n;
            }
            if (read < 4) return -1;

            // Simple byte search.
            for (int i = 0; i <= read - 4; i++)
            {
                if (buffer[i] == OggS[0] && buffer[i + 1] == OggS[1] && buffer[i + 2] == OggS[2] && buffer[i + 3] == OggS[3])
                    return i;
            }
            return -1;
        }

        private static async Task ExtractFromOffsetAsync(string sourcePath, string outPath, long offset, CancellationToken ct)
        {
            using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 128 * 1024, useAsync: true);
            using var dst = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read,
                bufferSize: 128 * 1024, useAsync: true);

            if (offset > 0)
                src.Seek(offset, SeekOrigin.Begin);

            await src.CopyToAsync(dst, 128 * 1024, ct).ConfigureAwait(false);
            await dst.FlushAsync(ct).ConfigureAwait(false);
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            if (string.IsNullOrWhiteSpace(name))
                name = "audio";
            if (name.Length > 80)
                name = name.Substring(0, 80);
            return name;
        }
    }
}
