using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Runtime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EscapeFromTheWoods
{
    class Program
    {
        static async Task Main(string[] args)
        {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            DBwriter db = new DBwriter();

            string path = @"C:\Users\bertd\Documents\Hogent 2023-2024\Sem1\Programmeren specialisatie\Opdrachten\Opdracht3_EscapeTheWoods\EscapeTheWoods\Bitmaps\";

            Map m1 = new Map(0, 500, 0, 500);
            Wood w1 = WoodBuilder.GetWood(500, m1, path, db);
            w1.PlaceMonkey("Alice", IDgenerator.GetMonkeyID());     // No changes, best run synchronous to ensure 
            w1.PlaceMonkey("Janice", IDgenerator.GetMonkeyID());    // no 2 monkeys are in the same tree
            w1.PlaceMonkey("Toby", IDgenerator.GetMonkeyID());
            w1.PlaceMonkey("Mindy", IDgenerator.GetMonkeyID());
            w1.PlaceMonkey("Jos", IDgenerator.GetMonkeyID());

            Map m2 = new Map(0, 200, 0, 400);
            Wood w2 = WoodBuilder.GetWood(2500, m2, path, db);
            w2.PlaceMonkey("Tom", IDgenerator.GetMonkeyID());
            w2.PlaceMonkey("Jerry", IDgenerator.GetMonkeyID());
            w2.PlaceMonkey("Tiffany", IDgenerator.GetMonkeyID());
            w2.PlaceMonkey("Mozes", IDgenerator.GetMonkeyID());
            w2.PlaceMonkey("Jebus", IDgenerator.GetMonkeyID());

            Map m3 = new Map(0, 400, 0, 400);
            Wood w3 = WoodBuilder.GetWood(2000, m3, path, db);
            w3.PlaceMonkey("Kelly", IDgenerator.GetMonkeyID());
            w3.PlaceMonkey("Kenji", IDgenerator.GetMonkeyID());
            w3.PlaceMonkey("Kobe", IDgenerator.GetMonkeyID());
            w3.PlaceMonkey("Kendra", IDgenerator.GetMonkeyID());



            using (var session = await db._db.Client.StartSessionAsync())
            {
                try
                {
                    session.StartTransaction();

                    // Execute parallel, but wait until they are all finished to continue
                    Task t1 = w1.WriteWoodToDB();
                    Task t2 = w2.WriteWoodToDB();
                    Task t3 = w3.WriteWoodToDB();
                    await Task.WhenAll(t1, t2, t3);

                    // Same
                    Task t4 = w1.EscapeAsync();
                    Task t5 = w2.EscapeAsync();
                    Task t6 = w3.EscapeAsync();
                    await Task.WhenAll(t4, t5, t6);

                    await session.CommitTransactionAsync();
                } catch (Exception ex)
                {
                    Console.WriteLine($"Transaction failed: {ex.Message}");
                    await session.AbortTransactionAsync();
                }
            }

            stopwatch.Stop();
            // Write result.
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
            Console.WriteLine("end");
        }
    }
}
