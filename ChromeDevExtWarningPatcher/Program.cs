using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

// Ugly and uncommented code ahead
namespace ChromeDevExtWarningPatcher
{
    class Program
    {
        private const string CHROME_INSTALLATION_FOLDER = @"C:\Program Files (x86)\Google\Chrome\Application";

        private static readonly byte[] SHOULDINCLUDEEXTENSION_FUNCTION_PATTERN = { 0x56, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x89, 0xD6, 0x48, 0x89, 0xD1, 0xE8, 0xFF, 0xFF, 0xFF, 0xFF, 0x89, 0xC1 }; // 0xFF is ?


        private static readonly BytePatch[] BYTE_PATCHES = { new BytePatch(SHOULDINCLUDEEXTENSION_FUNCTION_PATTERN, 0x04, 0xFF, 22), new BytePatch(SHOULDINCLUDEEXTENSION_FUNCTION_PATTERN, 0x08, 0xFF, 35) };

        private static double GetUnixTime(DateTime date)
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static void Main(string[] args)
        {
            DirectoryInfo chromeFolder = new DirectoryInfo(CHROME_INSTALLATION_FOLDER);

            List<DirectoryInfo> chromeVersions = new List<DirectoryInfo>(chromeFolder.EnumerateDirectories());
            chromeVersions = chromeVersions.OrderByDescending(dirInfo => GetUnixTime(dirInfo.LastWriteTime)).ToList();
            foreach (DirectoryInfo dirs in chromeVersions)
            {
                if(dirs.Name.Contains("."))
                {
                    Console.WriteLine("Found chrome installation: " + dirs.Name);

                    foreach(FileInfo file in dirs.EnumerateFiles())
                    {
                        if(file.Name.Equals("chrome.dll"))
                        {
                            Console.WriteLine("Found chrome.dll: " + file.FullName);
                            Console.WriteLine("Patching successful?: " + BytePatchChrome(file));
                            break;
                        }
                    }

                    break;
                }
            }

            Thread.Sleep(5000); // Wait a bit to let the user see the result
        }

        private static bool BytePatchChrome(FileInfo chromeDll)
        {
            byte[] chromeBytes = File.ReadAllBytes(chromeDll.FullName);

            foreach (BytePatch bytePatch in BYTE_PATCHES)
            {
                int patternIndex = 0, patternOffset = 0;
                bool foundPattern = false;

                for (int i = 0; i < chromeBytes.Length; i++)
                {
                    byte chromeByte = chromeBytes[i];
                    byte expectedByte = bytePatch.pattern[patternIndex];

                    if (expectedByte == 0xFF ? true : (chromeByte == expectedByte))
                        patternIndex++;
                    else
                        patternIndex = 0;

                    if (patternIndex == bytePatch.pattern.Length)
                    {
                        foundPattern = true;
                        patternOffset = i - (patternIndex - 1);
                        Console.WriteLine("Found pattern offset at " + patternOffset);
                        break;
                    }
                }

                if (!foundPattern)
                {
                    Console.WriteLine("Pattern not found!");
                    return false;
                }
                else
                {
                    bool patched = false;
                    int index = patternOffset + bytePatch.offset;
                    byte sourceByte = chromeBytes[index];

                    Console.WriteLine("Source byte of patch at " + bytePatch.offset + ": " + sourceByte);
                    if (sourceByte == bytePatch.origByte)
                    {
                        chromeBytes[index] = bytePatch.patchByte;
                        Console.WriteLine(index + " => " + bytePatch.patchByte);
                        patched = true;
                    }
                    else
                        Console.WriteLine("Source byte unexpected, should be " + bytePatch.origByte + "!");

                    if (patched)
                    {
                        File.WriteAllBytes(chromeDll.FullName, chromeBytes);
                        Console.WriteLine("Patched one byte in " + chromeDll.FullName);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}