using Scout;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main()
        {
            Scout.VolumeManager.Init();
            Console.WriteLine(Scout.VolumeManager.VolumeCount);
            Console.ReadKey();
        }
    }
}