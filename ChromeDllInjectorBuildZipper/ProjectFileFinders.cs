using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromeDllInjectorBuildZipper;

public class ProjectFileFinders {
	public static string FindBuildFolder() {
		FileInfo builtInjector = FindAllInjectorExecutables(new DirectoryInfo(".")).OrderByDescending(file => {
			DateTimeOffset lastWriteOffset = file.LastWriteTimeUtc;
			return lastWriteOffset.ToUnixTimeSeconds();
		}).First();

		string buildFolder = Directory.GetParent(builtInjector.FullName)!.FullName;
		Console.WriteLine("Found build folder at: " + buildFolder);
		return buildFolder;
	}

	public static FileInfo? FindSignExecutor(DirectoryInfo dir) {
		foreach (FileInfo file in dir.EnumerateFiles()) {
			if (file.Name == "signall.bat") {
				return file;
			}
		}

		DirectoryInfo? parentDir = dir.Parent;
		return parentDir is not null ? FindSignExecutor(parentDir) : null;
	}

	private static IEnumerable<FileInfo> FindAllInjectorExecutables(DirectoryInfo dir) {
		foreach (FileInfo file in dir.EnumerateFiles()) {
			if (file.Name == "ChromeDllInjector.exe") {
				yield return file;
			}
		}

		foreach (DirectoryInfo foundDir in dir.EnumerateDirectories()) {
			foreach (FileInfo file in FindAllInjectorExecutables(foundDir)) {
				yield return file;
			}
		}
	}
}
