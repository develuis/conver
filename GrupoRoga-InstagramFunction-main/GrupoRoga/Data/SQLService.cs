using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga.Data
{
    public class SQLService
    {
        private string _connectionString;
        public string SCHEMA;

        public SQLService()
        {
            AppSettings appSettings = new AppSettings();
            _connectionString = appSettings.AZURE_SQL_CONECTION_STRING ?? "";
            SCHEMA = appSettings.AZURE_SQL_SCHEMA ?? "";
        }

        public int Insert(JObject fields, string table)
        {
            string horaInicioInsercion = DateTime.Now.ToString();

            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    cnn.Open();

                    List<string> _fields = new List<string>() { };
                    List<string> _values = new List<string>() { };

                    foreach (var item in fields)
                    {
                        _fields.Add("[" + item.Key + "]");
                        _values.Add("'" + item.Value + "'");
                    }

                    string sql = string.Format("INSERT INTO {0}({1}) VALUES({2})", table, String.Join(", ", _fields.ToArray()), String.Join(", ", _values.ToArray()));

                    using (SqlCommand cmd = new SqlCommand(sql, cnn))
                    {
                        var horaFinInsercion = DateTime.Now.ToString();
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en Guardado BD. Message:" + ex.Message + "\n Stack: " + ex.InnerException);
                    return 0;
                }
            }
        }

        public List<JObject> Query(string sql)
        {
            List<JObject> results = new List<JObject>();
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    cnn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, cnn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            foreach (DataRow row in dt.Rows)
                            {
                                JObject row_object = new JObject() { };
                                foreach (DataColumn column in dt.Columns)
                                {
                                    row_object.Add(column.ColumnName, row[column].ToString());
                                }
                                results.Add(row_object);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            return results;
        }

        public int Update(JObject fields, string where_clause, string table)
        {
            //table = $"{SCHEMA}.{table}";
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    cnn.Open();
                    List<string> fields_statement = new List<string>() { };

                    foreach (var item in fields)
                        fields_statement.Add(string.Format(" [{0}] = '{1}'", item.Key, item.Value.ToString().Replace("'", "")));

                    string statement = String.Join(", ", fields_statement.ToArray()),
                        sql = string.Format("UPDATE {0} SET {1} WHERE {2}", table, statement, where_clause);

                    using (SqlCommand cmd = new SqlCommand(sql, cnn))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 0;
                }
            }
        }
    }
}
