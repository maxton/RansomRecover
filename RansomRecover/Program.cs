using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RansomRecover
{
  class Program
  {
    static void Main(string[] args)
    {
      if(args.Length < 3)
      {
        Console.WriteLine("Usage: RansomRecover.exe Extension TargetDir SourceDir [report.txt]");
      }
      string pattern = args[0];
      string targetDir = args[1];
      string sourceDir = args[2];
      var files = Directory.EnumerateFiles(targetDir, "*"+pattern, SearchOption.AllDirectories);
      Console.WriteLine("{0} encrypted files.", files.Count());
      var len = targetDir.Length;
      var matchedFiles = new List<(string src, string target)>();
      var unmatchedFiles = new List<string>();
      long matchedFilesSize = 0;
      long unmatchedFilesSize = 0;
      long totalEncryptedSize = 0;
      foreach (var f in files)
      {
        var relativeName = f.Remove(f.Length - pattern.Length).Substring(len + 1);
        var sourceName = Path.Combine(sourceDir, relativeName);
        var length = new FileInfo(f).Length;
        totalEncryptedSize += length;
        if (File.Exists(sourceName))
        {
          matchedFiles.Add((sourceName, Path.Combine(targetDir, relativeName)));
          matchedFilesSize += new FileInfo(sourceName).Length;
        }
        else
        {
          unmatchedFiles.Add(f);
          unmatchedFilesSize += length;
        }
      }
      var encryptedCount = files.Count();
      Console.WriteLine(
        "Found {0} matches and {1} unmatched. {2:0.00}% recovery",
        matchedFiles.Count,
        unmatchedFiles.Count,
        100.0 * matchedFiles.Count / encryptedCount);
      Console.WriteLine("");
      var errors = new List<string>();
      int i = 0;
      foreach(var (src, target) in matchedFiles)
      {
        Console.Write('\r');
        Console.Write(new string(' ', Console.BufferWidth - 1));
        Console.Write("\rRestoring ({1}/{2}): {0}", target, ++i, matchedFiles.Count);
        try
        {
          if (File.GetAttributes(src).HasFlag(FileAttributes.ReadOnly))
          {
            File.SetAttributes(src, File.GetAttributes(src) & ~FileAttributes.ReadOnly);
          }
          File.Copy(src, target);
          File.SetCreationTimeUtc(target, File.GetCreationTimeUtc(src));
          File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(src));
          File.SetLastAccessTimeUtc(target, File.GetLastAccessTimeUtc(src));
          if (new FileInfo(src).Length == new FileInfo(target).Length)
          {
            var badFile = target + pattern;
            File.SetAttributes(badFile, FileAttributes.Normal);
            File.Delete(badFile);
          }
        }
        catch (Exception e)
        {
          errors.Add(e.Message);
          Console.WriteLine("\r\nError!: " + e.Message);
        }
      }
      if(args.Length == 4)
      {
        using (var f = File.OpenWrite(args[3]))
        using (var sw = new StreamWriter(f))
        {
          sw.WriteLine("== Summary ==");
          sw.WriteLine("Source: {0}", sourceDir);
          sw.WriteLine("Target: {0}", targetDir);
          sw.WriteLine(
            "{0} matches and {1} unmatched. {2:0.00}% recovery",
            matchedFiles.Count,
            unmatchedFiles.Count,
            100.0 * matchedFiles.Count / encryptedCount);
          sw.WriteLine("Encrypted files size: {0}", totalEncryptedSize);
          sw.WriteLine("Matched files size:   {0}", matchedFilesSize);
          sw.WriteLine("Unmatched files size: {0}", unmatchedFilesSize);
          sw.WriteLine("=============================================");
          sw.WriteLine("Errors:\r\n");
          foreach(var e in errors)
          {
            sw.WriteLine(e);
          }
          sw.WriteLine("\r\nUnmatched files:\r\n");
          foreach(var uf in unmatchedFiles)
          {
            sw.WriteLine(uf);
          }
        }
      }
    }
  }
}
