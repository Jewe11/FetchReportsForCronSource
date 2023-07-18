using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UVACanvasAccess.ApiParts;

namespace FetchReportsForCron
{
    public class FetchCsvs
    {
         public static async Task GetCsvs()
        {
            var token = Program.Config("token").ToString().Trim();
            var rootDirectory = new DirectoryInfo(Program.Config("rootPath").ToString().Trim());
            //Console.WriteLine(token);

            var api = new Api(token,
                Program.Config("baseURL").ToString());
            var termNameFilter = Program.Config("termFilter").ToString() == string.Empty ? null : Program.Config("termFilter").ToString().Trim().Split(',');

            var csvNameFilter =  Program.Config("csvFilter").ToString() == string.Empty ? null : Program.Config("csvFilter").ToString().Trim().Split(',');

            var currTermsGp = api.StreamEnrollmentTerms()
                .Where(ter => ter.EndAt > DateTime.Today && ter.GradingPeriodGroupId is not null);
            var currTermsNoGp = api.StreamEnrollmentTerms()
                .Where(ngp => ngp.EndAt > DateTime.Today && ngp.GradingPeriodGroupId is null);

            if (currTermsGp != null)
            {
                await foreach (var term in currTermsGp)
                {
                    if (termNameFilter != null && termNameFilter.Any(word => Regex.IsMatch(term.Name, @$"\b{word}\b", RegexOptions.IgnoreCase)))
                        continue;
                    Console.WriteLine(term.ToPrettyString());
                    await Task.Run(async () =>
                    {
                        var r = await api.StartReport("mgp_grade_export_csv",
                            new[]
                            {
                                ("enrollment_term_id", (object) term.Id)
                            });
                        while ("complete" != r.Status)
                        {
                            await Task.Delay(1000 * 30);
                            r = await r.Refresh();
                        }

                        var data = await r.Attachment.Download();
                        //file name
                        var path =
                            rootDirectory + "\\mgp.zip";
                        if (data != null) 
                            File.WriteAllBytes(path,
                                data);
                        Console.WriteLine(term.Id); 
                        Unzip(term.Id);
                        //Console.WriteLine("Run Unzip Method");
                    });
                }
            }
            if (currTermsNoGp != null)
            {
                await foreach (var term in currTermsNoGp)
                {
                    if (csvNameFilter != null && !csvNameFilter.Any(word =>
                            Regex.IsMatch(term.Name, @$"\b{word}\b", RegexOptions.IgnoreCase)))
                    if (termNameFilter != null && termNameFilter.Any(word => 
                            Regex.IsMatch(term.Name, @$"\b{word}\b", RegexOptions.IgnoreCase))) 
                        continue;
                    
                    Console.WriteLine(term.ToPrettyString());
                    
                    await Task.Run(async () =>
                    {
                        var r = await api.StartReport("grade_export_csv",
                            new[]
                            {
                                ("enrollment_term_id", (object) term.Id)
                            });
                        while ("complete" != r.Status)
                        {
                            await Task.Delay(1000 * 30);
                            r = await r.Refresh();
                        }

                        var data = await r.Attachment.Download();
                        //file name
                        var path = rootDirectory + "\\" + term.Id + ".csv";
                        if (data != null)
                            File.WriteAllBytes(path,
                                data);
                        //Console.WriteLine();
                        Console.WriteLine(term.Id + " -wrote csv");
                    });
                }
            }
        }

        private static void Unzip(ulong termId)
        {
            var terId = termId.ToString();
            var zipPath = Program.Config("rootPath") + "\\mgp.zip";
            var extractPath = Program.Config("rootPath").ToString();
            if (new FileInfo(zipPath).Length == 0)
            {
                // empty
                File.Delete(zipPath);
            }
            else
            {
                var zip = ZipFile.OpenRead(zipPath);
                foreach (var entry in zip.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, terId + ".csv"));
                    entry.ExtractToFile(destinationPath, true);
                    zip.Dispose();
                }
            }

            //Console.WriteLine("Extracted");
        }
    }
}