using System;

namespace ContributorRoleBot
{
    class Program
    {
        static void Main()
        {
            ContribHelper.Init();
            foreach (var keyValuePair in ContribHelper.ContributorActivity)
            {
                Console.WriteLine($"{keyValuePair.Key} is {(keyValuePair.Value ? "" : "in")}active.");
            }

            Console.ReadKey(true);
        }
    }
}
