using System.IO;

namespace UESoundExtractor.utils;

public class WemExtractor {

    public static void ExtractNameMap(string asset, Dictionary<string, string> map) {
        var audioFiles = new List<string>();

        foreach (var kvp in map) {
            audioFiles.Add(Extract(kvp.Key, kvp.Value));
        }
        
        String outputFilePath = Settings.settings.OutputFolder + "/" + asset.Split("/").Last().Replace(".uasset", "") + ".wav";
        String inputFiles = String.Join(" ", audioFiles.Select(f => $"-i \"{f}\""));
        String filterComplex = $"-filter_complex \"{String.Join("", audioFiles.Select((f, i) => $"[{i}:a]")).TrimEnd(':')}amix=inputs={audioFiles.Count}:duration=longest\"";
        String cmd2 = $"{inputFiles} {filterComplex} -c:a pcm_s16le \"{outputFilePath}\"";

        Console.WriteLine("Running command: " + cmd2);

        System.Diagnostics.Process p2 = System.Diagnostics.Process.Start("ffmpeg.exe", cmd2);
      //  p2.StartInfo.RedirectStandardOutput = true;
        p2.WaitForExit();
    }
    
    public static String Extract(String pkgPath, String wavName) {
        String filePath = Settings.settings.OutputFolder + "/" + wavName.Replace("ShooterGame/Content/WwiseAudio/", "");
        String wemFilePath = filePath + ".wem";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        byte[] data = Service.provider.SaveAsset(pkgPath);
        File.WriteAllBytes(wemFilePath, data);
        
        String wavPath = filePath + (filePath.EndsWith(".wav") ? "" : ".wav");

        String cmd = $"-o \"{wavPath}\" \"{wemFilePath}\"";
        Console.WriteLine("Running command: " + cmd);

        System.Diagnostics.Process p = System.Diagnostics.Process.Start("vgm/vgmstream-cli.exe", cmd);
      //  p.StartInfo.RedirectStandardOutput = true;
        p.WaitForExit();

        // delete the temporary .wem file
        File.Delete(wemFilePath);
        
        return wavPath;
    }
}