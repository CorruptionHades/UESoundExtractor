using System.IO;
using System.Text.RegularExpressions;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using UESoundExtractor.utils;

namespace UESoundExporter;

public static class BnkExtractor {
    
    public static HashSet<String> GetPossibleMediaIDS(string bnkPkgPath) {
        String filePath = Settings.settings.OutputFolder + "/" 
                    + bnkPkgPath.Replace("ShooterGame/Content/WwiseAudio/", "");
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        byte[] data = Service.provider.SaveAsset(bnkPkgPath);
        File.WriteAllBytes(filePath, data);
        
        // python wwiser/wwiser.py Play_UI_KillBanner_SoulStealer_5.bnk
        String cmd = $"wwiser/wwiser.py \"{filePath}\"";
        Console.WriteLine("Running command: " + cmd);
        
        System.Diagnostics.Process p = System.Diagnostics.Process.Start("python", cmd);
        p.WaitForExit();
        
        String content = File.ReadAllText(filePath + ".xml");
        
        // get matches for // na="sourceID" va="(\d+)"
        var mediaIDs = new HashSet<String>();
        
        foreach (Match match in Regex.Matches(content, "na=\"sourceID\" va=\"(\\d+)\"")) {
            mediaIDs.Add(match.Groups[1].Value);
        }
        
        return mediaIDs;
    }
}