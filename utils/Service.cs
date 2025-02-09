using System.IO;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace UESoundExtractor.utils;

public static class Service {

    public static DefaultFileProvider provider;

    public static void init() {
        provider = new DefaultFileProvider(Settings.settings.PaksFolder, SearchOption.AllDirectories, 
            true, new VersionContainer(EGame.GAME_Valorant));
        provider.Initialize(); // will scan the archive directory for supported file extensions
        provider.SubmitKey(new FGuid(), new FAesKey(Settings.settings.AesKey));
    }
    
     public static List<string> getAllAudio() {
        Dictionary<string, GameFile> audioFiles = new();

        int count = 0;
        // search for .wem and .bnk files
        foreach (var file in Service.provider.Files) {

            if (count == 100) {
            //    break;
            }
            
            if (file.Key.EndsWith(".wem") || file.Key.EndsWith(".bnk")) {
                audioFiles.Add(file.Key, file.Value);
                count ++;
            }
        }
        
        Console.WriteLine("Found " + audioFiles.Count + " audio files.");
        
        return audioFiles.Keys.ToList();
    }

    public static List<string> GetEventFolders() {
        var eventFolderPaths = Settings.settings.EventFolders;
        
        var eventFolders = new List<string>();
        
        foreach (var folder in eventFolderPaths) {
            foreach (var keyValuePair in Service.provider.Files) {
                if (keyValuePair.Key.StartsWith(folder)) {
                    eventFolders.Add(keyValuePair.Key);
                }
            }
        }
        
        return eventFolders;
    }
    
    public static List<string> GetFilesInEventFolder(string folderPath)
    {
        var audioFiles = new List<string>();
        
        foreach (var file in Service.provider.Files)
        {
            if (file.Key.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            {
                audioFiles.Add(file.Key);
            }
        }
        
        return audioFiles;
    }

    public static Dictionary<string, string> LoadNameMap(String pkgPath) {
        var pkg = Service.provider.LoadPackage(pkgPath);
        
        // Extract the NameMap array
        var nameMap = pkg.NameMap
                .Select(n => n.Name)
                .ToList();
        
        var wemToWavMap = new Dictionary<string, string>();
        
        var wemFiles = nameMap
            .Where(n => n.EndsWith(".wem"))
            .ToList();
        
        var wavFiles = nameMap
            .Where(n => n.EndsWith(".wav"))
            .ToList();
        
        for (int i = 0; i < wemFiles.Count; i++) {
            String wav;

            try {
                wav = wavFiles[i];
            }
            catch (ArgumentOutOfRangeException e) {
                wav = wemFiles[i];
            }

            wemToWavMap.Add(wemFiles[i], wav);
        }
        
        return wemToWavMap;
    }
}