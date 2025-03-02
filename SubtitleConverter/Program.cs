﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SubtitleConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run <folderPath>");
                return;
            }

            string folderPath = args[0];

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"The folder '{folderPath}' does not exist.");
                return;
            }

            var assFiles = Directory.GetFiles(folderPath, "*.ass");
            if (assFiles.Length == 0)
            {
                Console.WriteLine("No .ass files found in the specified folder.");
                return;
            }

            foreach (var file in assFiles)
            {
                try
                {
                    List<string> srtLines = new List<string>();
                    int counter = 1;
                    bool inEvents = false;
                    var lines = File.ReadAllLines(file);

                    foreach (var line in lines)
                    {
                        if (line.Trim().Equals("[Events]", StringComparison.OrdinalIgnoreCase))
                        {
                            inEvents = true;
                            continue;
                        }

                        if (!inEvents)
                        {
                            continue;
                        }

                        if (line.StartsWith("Dialogue:", StringComparison.OrdinalIgnoreCase))
                        {
                            // Remove the "Dialogue:" prefix.
                            string dialogueContent = line.Substring("Dialogue:".Length).Trim();
                            // Split into maximum 10 segments (the first 9 commas separate fixed fields, the rest is text).
                            var parts = dialogueContent.Split(new char[] { ',' }, 10);
                            if (parts.Length < 10)
                                continue; // Skip if format is unexpected.

                            string startTime = ConvertTime(parts[1].Trim());
                            string endTime = ConvertTime(parts[2].Trim());
                            string text = Regex.Replace(parts[9].Trim(), @"\{.*?\}", "");

                            srtLines.Add(counter.ToString());
                            srtLines.Add($"{startTime} --> {endTime}");
                            srtLines.Add(text);
                            srtLines.Add(string.Empty);
                            counter++;
                        }
                    }

                    string outputPath = Path.ChangeExtension(file, ".srt");
                    File.WriteAllLines(outputPath, srtLines);
                    Console.WriteLine($"Converted: {file} -> {outputPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to convert {file}: {ex.Message}");
                }
            }
        }

        // Convert ASS time format (H:MM:SS.CC) to SRT time format (HH:MM:SS,mmm)
        static string ConvertTime(string assTime)
        {
            var parts = assTime.Split(new char[] { ':', '.' });
            if (parts.Length < 4)
                return "00:00:00,000";

            int hours = int.Parse(parts[0].Trim());
            int minutes = int.Parse(parts[1].Trim());
            int seconds = int.Parse(parts[2].Trim());
            int centiseconds = int.Parse(parts[3].Trim());
            int milliseconds = centiseconds * 10;

            return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
        }
    }
}
