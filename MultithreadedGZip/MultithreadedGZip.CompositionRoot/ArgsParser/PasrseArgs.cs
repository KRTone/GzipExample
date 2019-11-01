using System;
using System.IO.Compression;

namespace MultithreadedGZip.CompositionRoot.ArgsParser
{
    class PasrseArgs
    {
        public static CompressionMode GetCompressionMode(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "compress":
                    return CompressionMode.Compress;
                case "decompress":
                    return CompressionMode.Decompress;
                default:
                    throw new ArgumentException(nameof(CompressionMode));
            }
        }

        public static string GetInFilePath(string[] args)
        {
            return GetPath(args, PathType.InFile);
        }

        public static string GetOutFilePath(string[] args)
        {
            return GetPath(args, PathType.OutFile);
        }

        static string GetPath(string[] args, PathType pathType)
        {
            switch (pathType)
            {
                case PathType.InFile:
                    return args[1];
                case PathType.OutFile:
                    return args[2];
                default:
                    throw new ArgumentException(nameof(pathType));
            }
        }

        public static void ThrowIfBadArgs(string[] args)
        {
            if (args == null || args.Length != 3)
                throw new ArgumentException("Incorrect input args");
        }
    }
}
