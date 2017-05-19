using System;
using System.Diagnostics;
using System.IO.Compression;

namespace System.IO.Compression.Test
{
    class Program
    {
        static void Compress(String path_in, String path_out)
        {
            Byte[] input = File.ReadAllBytes(path_in);
            Byte[] output = null;
            using (System.IO.MemoryStream msInput = new System.IO.MemoryStream(input))
            using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream())
            using (BrotliStream bs = new BrotliStream(msOutput, System.IO.Compression.CompressionMode.Compress, false, 22, 11))
            {
                msInput.CopyTo(bs);
                bs.Close();
                output = msOutput.ToArray();
            }
            File.WriteAllBytes(path_out, output);
        }
        static void Decompress(String path_in, String path_out)
        {
            Byte[] input = File.ReadAllBytes(path_in);
            Byte[] output = null;
            using (System.IO.MemoryStream msInput = new System.IO.MemoryStream(input))
            using (BrotliStream bs = new BrotliStream(msInput, System.IO.Compression.CompressionMode.Decompress))
            using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream())
            {
                bs.CopyTo(msOutput);
                msOutput.Seek(0, System.IO.SeekOrigin.Begin);
                output = msOutput.ToArray();
            }
            File.WriteAllBytes(path_out, output);
        }
        static void Main(string[] args)
        {
            //Console.WriteLine(Process.GetCurrentProcess().Id);
            //Console.ReadKey();
            /*Byte[] input = File.ReadAllBytes("input.txt");
            Byte[] output = null;
            using (System.IO.MemoryStream msInput = new System.IO.MemoryStream(input))
            using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream())
            using (BrotliStream bs = new BrotliStream(msOutput, System.IO.Compression.CompressionMode.Compress, false, 22, 11))
            {
                msInput.CopyTo(bs);
                bs.Close();
                output = msOutput.ToArray();
            }*/
            //File.WriteAllBytes(path_out, output);
            Compress("input.txt", "output.br");
            Decompress("output.br", "output.txt");


            Console.WriteLine("Nice end!");
        }
    }
}
