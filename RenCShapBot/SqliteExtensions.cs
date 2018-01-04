using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenCShapBot
{
    static class SqliteExtensions
    {
        public static void ExecuteNonQuery(this SQLiteConnection connection, string commandText)
        {
            // Ensure we have a connection
            if (connection == null)
            {
                throw new NullReferenceException("Please provide a connection");
            }

            // Ensure that the connection state is Open
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Use Dapper to execute the given query
            connection.Execute(commandText);
        }

        /* public static List<T> GetAll<T>(this SQLiteConnection connection)
        {
            return connection.GetAll<T>().ToList();
        } */
    }
}
