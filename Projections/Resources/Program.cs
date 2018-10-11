using System;
using System.Text;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ptv.Components.Projections.Resources
{
    class Program
    {
        static bool dumpArgs = false;

        static void write(string output)
        {
            foreach (string line in output.Split('\n', '\r'))
                if (line.Trim().Length > 0)
                Console.WriteLine("RESOURCE PACKAGER: " + line);
        }

        static string CompressWkt(ref string lastWkt, string Wkt)
        {
            if (Wkt.IndexOf(';') >= 0)
                throw new Exception("wkt contains invalid characters");

            if (String.Compare(lastWkt, Wkt) == 0)
                return "" + lastWkt.Length + ";;0";

            int i = 0, j = 0;

            while (i < Wkt.Length && i < lastWkt.Length && lastWkt[i] == Wkt[i])
                i++;

            while (j < Wkt.Length && j < lastWkt.Length && Wkt.Length - 1 - j >= i && lastWkt[lastWkt.Length - 1 - j] == Wkt[Wkt.Length - 1 - j])
                j++;

            int left = i;
            string mid = Wkt.Substring(i, Wkt.Length - i - j);
            int right = j;

            if (String.Compare(lastWkt.Substring(0, left) + mid + lastWkt.Substring(lastWkt.Length - j), Wkt) != 0)
                throw new Exception("compression failed");

            lastWkt = Wkt;

            return "" + left + ";" + mid + ";" + right;
        }

        static byte[] readAndReformatEPSGDatabase(string fn)
        {
            Regex streamParser = 
                new Regex("<([0-9]+)>(.*)<>", RegexOptions.Compiled);

            Match m = null;
            bool bEmpty = true;

            StringBuilder sb = new StringBuilder();

            string lastWkt = "";

            foreach (string line in File.ReadAllLines(fn))
                if (line.Trim().Length > 0 && !line.Trim().StartsWith("#"))
                    if ((m = streamParser.Match(line.Trim())).Success)
                    {
                        if (!bEmpty)
                            sb.Append('\n');

                        string code = m.Groups[1].Value.Trim();
                        string wkt = m.Groups[2].Value.Trim();

                        if (code.Length > 0 && wkt.Length > 0)
                        {
                            sb.Append(code);
                            sb.Append(';');
                            sb.Append(CompressWkt(ref lastWkt, wkt));
                        }

                        bEmpty = false;
                    }

            return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
        }

        static void Main(string[] args)
        {
            try
            {
                if (dumpArgs)
                {
                    write("args.Length = " + args.Length);

                    for (int i = 0; args != null && i < args.Length; ++i)
                        write("args[" + i + "] = " + args[i]);
                }

                bool buildRelease = String.Compare(args[0], "Release", true) == 0;

                string @base = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;
                string[] uri = null;

                if (buildRelease)
                    uri = new string[] {
                        "/epsg", "text/plain",  @"..\3rd Party\epsg\epsg.txt",
                        "/Proj.4-Core.x86.dll", "application/octet-stream", @"..\Core-x86\Release\Proj.4-Core.x86.dll",
                        "/Proj.4-Core.x64.dll", "application/octet-stream", @"..\Core-x64\Release\Proj.4-Core.x64.dll"
                    };
                else
                    uri = new string[] {
                        "/epsg", "text/plain",  @"..\3rd Party\epsg\epsg.txt",
                        "/Proj.4-Core.x86.dll", "application/octet-stream", @"..\Core-x86\Debug\Proj.4-Core.x86d.dll",
                        "/Proj.4-Core.x64.dll", "application/octet-stream", @"..\Core-x64\Debug\Proj.4-Core.x64d.dll"
                    };

                if (!buildRelease)
                    write("creating DEBUG resource package");
                else
                    write("creating RELEASE resource package");

                using (Package p = ZipPackage.Open(@base + @"resources.zip", FileMode.Create))
                {
                    for (int i = 0; i < uri.Length; i += 3)
                    {
                        PackagePart pp = p.CreatePart(new Uri(uri[i], UriKind.Relative), uri[i+1], CompressionOption.Maximum);

                        byte[] raw = (i == 0 ? readAndReformatEPSGDatabase(@base + uri[i + 2]) : 
                            File.ReadAllBytes(@base + uri[i + 2]));

                        write("+ " + uri[i + 2] + " (" + raw.Length / 1024 + "kB)");

                        using (Stream stm = pp.GetStream())
                            stm.Write(raw, 0, raw.Length);
                    }
                }

                write("size of resulting package: " +
                    (new FileInfo(@base + @"resources.zip").Length / 1024) + "kB");

                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                while (ex != null)
                {
                    write("failed with " + ex.GetType().FullName + ": " + ex.Message);
                    write(ex.StackTrace.ToString());

                    ex = ex.InnerException;
                }

                Environment.Exit(-1);
            }
        }
    }
}
