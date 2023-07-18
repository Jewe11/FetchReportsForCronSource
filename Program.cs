using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nett;

namespace FetchReportsForCron
{
    internal class Program
    {
        public static async Task Main()
        {
            var rootDirectory = new DirectoryInfo(Program.Config("rootPath").ToString().Trim());
            Console.WriteLine(rootDirectory.Exists);
            if (Config("purgeFilesBefore").ToString().Trim() == "true")
            {
                if (rootDirectory.Exists)
                {
                    await Empty(rootDirectory, "*csv");
                }
                
            }
            //-------
            await FetchCsvs.GetCsvs();
            //-------
            if (Config("purgeFilesAfter").ToString().Trim() == "true")
            {
                await Empty(rootDirectory, "*csv");
            }
        }
        public static TomlObject Config(string keyV)
        {
            //var workingDirectory =System.Reflection.Assembly.GetExecutingAssembly().Location;
            //var parentDirectory =  Directory.GetParent(workingDirectory)?.Parent?.FullName;
            TomlObject info = null;
            //Console.WriteLine(parentDirectory+"\\config.toml");
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var exeDir = Path.GetDirectoryName(exePath);
                //var binDir = System.IO.Directory.GetParent(exeDir ?? throw new InvalidOperationException());
                var parseToml = Toml.ReadFile(exeDir + "\\config.toml");
                var data = parseToml.Rows.AsQueryable().Where(x => x.Key == keyV);
                foreach (var keyValuePair in data)
                {
                    info = keyValuePair.Value;
                }
            }
            return info;
        }
        private static async Task Empty(DirectoryInfo directory, string ext)
        {
            await Task.Delay(1000);
            foreach(var file in directory.GetFiles($"{ext}")) file.Delete();
            Console.WriteLine($"Dumped {directory.Name}");
        }
    }
}