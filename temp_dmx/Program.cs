using System;
using Microsoft.AnalysisServices.AdomdClient;

class Program
{
    static void Main()
    {
        try
        {
            using (var conn = new AdomdConnection("Provider=MSOLAP;Data Source=localhost;Catalog=ClothingShop_Cube;"))
            {
                conn.Open();
                
                string categoryName = "Party & Cocktail Dresses";
                string escapedCategory = categoryName.Replace("]", "]]");
                
                string mdx1 = $@"
                    SELECT {{ [Measures].[Quantity] }} ON COLUMNS,
                    NON EMPTY TOPCOUNT(
                        {{ [Dim Product].[Product Key].Members }}, 
                        5, 
                        [Measures].[Quantity]
                    ) ON ROWS
                    FROM [{ "DB Shop Quan Ao Data Warehouse" }]
                    WHERE ( [Dim Product].[Category Name].[{escapedCategory}] )";
                
                Console.WriteLine("Executing MDX without ampersand:");
                ExecuteMdx(conn, mdx1);

                string mdx2 = $@"
                    SELECT {{ [Measures].[Quantity] }} ON COLUMNS,
                    NON EMPTY TOPCOUNT(
                        {{ [Dim Product].[Product Key].Members }}, 
                        5, 
                        [Measures].[Quantity]
                    ) ON ROWS
                    FROM [{ "DB Shop Quan Ao Data Warehouse" }]
                    WHERE ( [Dim Product].[Category Name].&[{escapedCategory}] )";
                
                Console.WriteLine("\nExecuting MDX with ampersand:");
                ExecuteMdx(conn, mdx2);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("MAIN ERROR: " + ex.Message);
        }
    }

    static void ExecuteMdx(AdomdConnection conn, string mdx)
    {
        try 
        {
            using (var cmd = new AdomdCommand(mdx, conn))
            {
                var cs = cmd.ExecuteCellSet();
                if (cs.Axes.Count < 2) 
                {
                    Console.WriteLine("No rows returned.");
                    return;
                }
                var rowAxis = cs.Axes[1];
                Console.WriteLine($"Found {rowAxis.Positions.Count} products.");
                for (var r = 0; r < rowAxis.Positions.Count; r++)
                {
                    var m = rowAxis.Positions[r].Members[0];
                    Console.WriteLine($"  Product: {m.Caption} | Qty: {cs[0, r].Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
