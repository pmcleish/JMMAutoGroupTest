using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using JMMServer;
using JMMServer.Tasks;
using static JMMServer.Tasks.AutoAnimeGroupCalculator;

namespace JMMAutoGroupTest
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("SQL Server connection string required as first parameter");
                return 1;
            }

            ILookup<int, AnimeRelation> relationMap = null;
            IDictionary<int, string> titleMap = null;
            IEnumerable<int> animeIds = null;

            try
            {
                using (SqlConnection con = new SqlConnection(args[0]))
                {
                    con.Open();
                    relationMap = LoadRelations(con);
                    titleMap = LoadMainTitles(con);
                    animeIds = LoadAllAniDBAnimeIds(con);
                    animeIds = LoadAnimeIdsFromSeries(con);
                }

                var autoGrpCalc = new AutoAnimeGroupCalculator(relationMap);

                var results = animeIds.Select(id => new { AnimeId = id, GroupId = autoGrpCalc.GetGroupAnimeId(id) })
                    .ToLookup(a => titleMap[a.GroupId], a => "[" + a.AnimeId + "] " + titleMap[a.AnimeId])
                    .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

                foreach (var result in results)
                {
                    Console.WriteLine(result.Key);

                    foreach (string anime in result)
                    {
                        Console.WriteLine("\t" + anime);
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e);
                Console.ResetColor();
            }

            return 0;
        }

        private static ILookup<int, AnimeRelation> LoadRelations(IDbConnection con)
        {
            using (IDbCommand cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT fromAnime.AnimeID, toAnime.AnimeID, fromAnime.AnimeType, toAnime.AnimeType, fromAnime.MainTitle, toAnime.MainTitle, rel.RelationType
                        FROM AniDB_Anime_Relation rel
                            INNER JOIN AniDB_Anime fromAnime
                                ON fromAnime.AnimeID = rel.AnimeID
                            INNER JOIN AniDB_Anime toAnime
                                ON toAnime.AnimeID = rel.RelatedAnimeID";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    var relationMap = reader.EnumerateRecords()
                        .Select(r =>
                            {
                                var relation = new AnimeRelation
                                    {
                                        FromId = r.GetInt32(0),
                                        ToId = r.GetInt32(1),
                                        FromType = (AnimeTypes)r.GetInt32(2),
                                        ToType = (AnimeTypes)r.GetInt32(3),
                                        FromMainTitle = r.GetString(4),
                                        ToMainTitle = r.GetString(5)
                                    };

                                switch (r.GetString(6).ToLowerInvariant())
                                {
                                    case "full story":
                                        relation.RelationType = AnimeRelationType.FullStory;
                                        break;
                                    case "summary":
                                        relation.RelationType = AnimeRelationType.Summary;
                                        break;
                                    case "parent story":
                                        relation.RelationType = AnimeRelationType.ParentStory;
                                        break;
                                    case "side story":
                                        relation.RelationType = AnimeRelationType.SideStory;
                                        break;
                                    case "prequel":
                                        relation.RelationType = AnimeRelationType.Prequel;
                                        break;
                                    case "sequel":
                                        relation.RelationType = AnimeRelationType.Sequel;
                                        break;
                                    case "alternative setting":
                                        relation.RelationType = AnimeRelationType.AlternativeSetting;
                                        break;
                                    case "alternative version":
                                        relation.RelationType = AnimeRelationType.AlternativeVersion;
                                        break;
                                    case "same setting":
                                        relation.RelationType = AnimeRelationType.SameSetting;
                                        break;
                                    case "character":
                                        relation.RelationType = AnimeRelationType.Character;
                                        break;
                                    default:
                                        relation.RelationType = AnimeRelationType.Other;
                                        break;
                                }

                                return relation;
                            })
                        .ToLookup(r => r.FromId);

                    return relationMap;
                }
            }
        }

        private static Dictionary<int, string> LoadMainTitles(IDbConnection con)
        {
            using (IDbCommand cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT AnimeID, MainTitle FROM AniDB_Anime";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    var titleMap = reader.EnumerateRecords()
                        .ToDictionary(r => r.GetInt32(0), r => r.GetString(1));

                    return titleMap;
                }
            }
        }

        private static IEnumerable<int> LoadAnimeIdsFromSeries(IDbConnection con)
        {
            using (IDbCommand cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT anime.AnimeID
                        FROM AnimeSeries series
                            INNER JOIN AniDB_Anime anime
                                ON anime.AnimeID = series.AniDB_ID";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    var seriesAnimeIds = reader.EnumerateRecords()
                        .Select(r => r.GetInt32(0))
                        .ToList();

                    return seriesAnimeIds;
                }
            }
        }

        private static IEnumerable<int> LoadAllAniDBAnimeIds(IDbConnection con)
        {
            using (IDbCommand cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT AnimeID FROM AniDB_Anime ORDER BY MainTitle";

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    var allAnimeIds = reader.EnumerateRecords()
                        .Select(r => r.GetInt32(0))
                        .ToList();

                    return allAnimeIds;
                }
            }
        }
    }
}