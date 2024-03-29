﻿using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DAO
{
    public abstract class CRUD<T> : ICRUD<T>
    {
        public abstract string SQL_UPDATE { get; }
        public abstract string SQL_INSERT { get; }
        public abstract string SQL_SELECT { get; }
        public abstract string SQL_SELECT_ALL { get; }
        public abstract string SQL_DROP { get; }
        public abstract string SQL_COLUMNS { get; }
        public abstract string SQL_SEQ_VALUE { get; }

        private OracleConnection _Connection;
        public OracleConnection Connection { get => _Connection; set => _Connection = value; }


        public virtual async Task<T> SelectId(int p_ID)
        {
            T output = default(T);
            using var command = new OracleCommand(SQL_SELECT, Connection);
            var prms = new Dictionary<string, object>();
            AddParametersID(prms, p_ID);
            foreach (var prm in prms)
                command.Parameters.Add(prm.Key, prm.Value);

            SaveCommand(command);
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    throw new Exception(DateTime.Now + ": Neexistuje zaznam s id '" + p_ID + "'");

                var dataTable = new DataTable();
                dataTable.Load(reader);

                output = GetDTOList(dataTable)[0];
            }
            return output;
        }

        public async Task<bool> Update(T p_Object)
        {
            using var command = new OracleCommand(SQL_UPDATE, Connection);
            var prms = new Dictionary<string, object>();
            AddParameters(prms, p_Object, false);
            foreach (var prm in prms)
                command.Parameters.Add(prm.Key, prm.Value);

            SaveCommand(command);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<int> Insert(T p_Object)
        {
            using var command = new OracleCommand(SQL_INSERT, Connection);
            var prms = new Dictionary<string, object>();
            AddParameters(prms, p_Object, false);
            foreach (var prm in prms)
                command.Parameters.Add(prm.Key, prm.Value);

            SaveCommand(command);
            await command.ExecuteNonQueryAsync();

            using var command2 = new OracleCommand(SQL_SEQ_VALUE, Connection);
            SaveCommand(command2);
            using (var reader = await command2.ExecuteReaderAsync())
            {
                var dataTable = new DataTable();
                dataTable.Load(reader);

                return GetSeqValue(dataTable);
            }
        }

        public virtual async Task<bool> DropId(int p_ID)
        {
            using var command = new OracleCommand(SQL_DROP, Connection);
            var prms = new Dictionary<string, object>();
            AddParametersID(prms, p_ID);
            foreach (var prm in prms)
                command.Parameters.Add(prm.Key, prm.Value);

            SaveCommand(command);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public virtual async Task<List<T>> SelectAll(bool p_UseXML = false)
        {
            using var command = new OracleCommand(SQL_SELECT_ALL, Connection);

            SaveCommand(command);
            using var reader = await command.ExecuteReaderAsync();

            var dataTable = new DataTable();
            dataTable.Load(reader);

            var list = GetDTOList(dataTable);

            if (p_UseXML)
                XML<List<T>>.Write(list);

            return list;
        }

        private void SaveCommand(OracleCommand p_Command)
        {
            var output = p_Command.CommandText;

            if (p_Command.Parameters != null && p_Command.Parameters.Count > 0)
                for (int i = 0; i < p_Command.Parameters.Count; i++)
                {
                    if(p_Command.Parameters[i].Value == null)
                        output = output.Replace(p_Command.Parameters[i].ParameterName, "''");
                    else
                    output = output.Replace(p_Command.Parameters[i].ParameterName, "'" + p_Command.Parameters[i].Value.ToString() + "'");
                }

            Trace.WriteLine(output);
        }

        public abstract List<T> GetDTOList(DataTable p_DataTable);
        public abstract int GetSeqValue(DataTable p_DataTable);
        public abstract T GetDTO(DataRow p_DataRow);
        public abstract void AddParameters(Dictionary<string, object> p_Command, T p_Object, bool p_UseID = true);
        public abstract void AddParametersID(Dictionary<string, object> p_Command, int p_ID);
    }

    public class XML<T> where T : IEnumerable
    {
        public static T Read()
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                Async = true
            };

            using XmlReader reader = XmlReader.Create(GetFileName(), settings);
            var serializer = new XmlSerializer(typeof(T));
            if (serializer.CanDeserialize(reader))
            {
                var p = serializer.Deserialize(reader);
                T products = (T)p;
                return products;
            }
            return default(T);
        }

        public static void Write(T t)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = UTF8Encoding.UTF8
            };

            using FileStream fs = new FileStream(GetFileName(), FileMode.OpenOrCreate);
            using XmlWriter wr = XmlWriter.Create(fs, settings);
            new XmlSerializer(typeof(T)).Serialize(wr, t);
        }

        private static string GetFileName()
        {
            Type type = typeof(T);
            var name = type.GetGenericArguments()[0].Name;
            return name + ".xml";
        }
    }
}
