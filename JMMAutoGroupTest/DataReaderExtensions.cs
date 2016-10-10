using System;
using System.Collections.Generic;
using System.Data;

namespace JMMAutoGroupTest
{
   public static class DataReaderExtensions
   {
      /// <summary>
      /// Enumerates the records in the <see cref="IDataReader"/>.
      /// </summary>
      /// <param name="reader">The reader to enumerate.</param>
      /// <returns>A sequence of <see cref="IDataRecord"/>s.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
      public static IEnumerable<IDataRecord> EnumerateRecords(this IDataReader reader)
      {
         if (reader == null)
            throw new ArgumentNullException(nameof(reader));

         while (reader.Read())
         {
            yield return reader;
         }
      }
   }
}