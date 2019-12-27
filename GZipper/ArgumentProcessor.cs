using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AM.GZipperLib;


namespace GZipper
{
    public class ArgumentProcessor
    {
        private string _helpPattern = @"^.?(\?|help)$";

        private string[] _args;

        private CompressionMode _mode;
        private string _inputFile;
        private string _outputFile;

        public ArgumentProcessor(string[] args)
        {
            _args = args;
        }

        public bool ParseArguments()
        {
            if (_args.Length == 0)
            {
                Console.WriteLine(@"Missing required parameters, type key '-help' for help");
                return false;
            }
            if (new Regex(_helpPattern).IsMatch(_args[0]))
            {
                ShowHelp();
                return false;
            }
            if (_args[0] == "compress" || _args[0] == "decompress")
            {
                if (_args.Length < 3)
                {
                    Console.WriteLine("Incorrect parameters, required: compress|decompress [input file name] [output file name]");
                    return false;
                }

                var inputOutputFileNames = string.Join(" ", _args.Skip(1));
                string[] parsedFileNames = inputOutputFileNames
                    .Trim(new[] { '[', ']' })
                    .Split(new[] { "] [" }, StringSplitOptions.RemoveEmptyEntries);

                if (parsedFileNames.Length < 2)
                    Console.WriteLine("Incorrect file paths, type key '-help' for help"); 

                if (!CheckFileName(parsedFileNames[0], true)) return false;
                if (!CheckFileName(parsedFileNames[1], false)) return false;

                _inputFile = parsedFileNames[0];
                _outputFile = parsedFileNames[1];
                _mode = _args[0] == "compress" ? CompressionMode.Compress : CompressionMode.Decompress;
                return true;
            }
            Console.WriteLine("Unknown parameters, type key '-help' for help");
            return false;
        }

        public CompressionConfig GetConfig() => new CompressionConfig(_inputFile, _outputFile, _mode);


        private bool CheckFileName(string fileName, bool requiredToExist)
        {
            string path = "";
            try
            {
                path = Path.GetFullPath(fileName);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }

            if (requiredToExist)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"File {path} doesn't exist");
                    return false;
                }
            }
            else
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating folder: {ex.Message}");
                        return false;
                    }
                }
            }

            return true;
        }

        private void ShowHelp()
        {
            Console.WriteLine("compress|decompress [input_file_name] [output_file_name]");
        }
    }
}
