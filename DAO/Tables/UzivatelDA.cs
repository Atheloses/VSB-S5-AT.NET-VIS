﻿using DTO;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.Tables
{
    public class UzivatelDA : CRUD<Uzivatel>
    {
        private static string _SQL_UPDATE = "UPDATE Uzivatel SET Jmeno=:Jmeno,Prijmeni=:Prijmeni," +
            "Email=:Email where ID=:ID";
        private static string _SQL_INSERT = "INSERT INTO Uzivatel (Prezdivka,Jmeno,Prijmeni,Email,PocetPodukolu,Aktivni,DatumRegistrace,HesloHash,NormPrezdivka) VALUES" +
            "(:Prezdivka,:Jmeno,:Prijmeni,:Email,:PocetPodukolu,:Aktivni,:DatumRegistrace,:HesloHash,:NormPrezdivka)";
        private string _SQL_SELECT = "SELECT " + _SQL_COLUMNS + " FROM Uzivatel uz WHERE uz.id=:ID";
        private string _SQL_SELECT_NP = "SELECT " + _SQL_COLUMNS + " FROM Uzivatel uz WHERE uz.normprezdivka=:NormPrezdivka";
        private static string _SQL_COLUMNS = "uz.id uz_id, uz.prezdivka uz_prezdivka, uz.jmeno uz_jmeno, uz.prijmeni uz_prijmeni," +
            " uz.email uz_email, uz.datumregistrace uz_datumregistrace, uz.pocetpodukolu uz_pocetpodukolu, uz.aktivni uz_aktivni," +
            " uz.heslohash uz_heslohash,uz.normprezdivka uz_normprezdivka";
        private static string _SQL_DROP = "DELETE FROM Uzivatel WHERE id=:ID";
        private string _SQL_SELECT_ALL = "SELECT " + _SQL_COLUMNS + " FROM Uzivatel uz";
        private string _SQL_SEQ_VALUE = "select uzivatel_seq.CURRVAL as value from dual";

        public override string SQL_SEQ_VALUE => _SQL_SEQ_VALUE;
        public override string SQL_SELECT_ALL => _SQL_SELECT_ALL;
        public override string SQL_UPDATE => _SQL_UPDATE;
        public override string SQL_INSERT => _SQL_INSERT;
        public override string SQL_SELECT => _SQL_SELECT;
        public override string SQL_DROP => _SQL_DROP;
        public override string SQL_COLUMNS => _SQL_COLUMNS;

        public UzivatelDA(OracleConnection p_Connection)
        {
            Connection = p_Connection;
        }

        [Obsolete("DropId is deprecated, please use Deaktivate instead")]
        public new async Task<bool> DropId(int p_ID)
        {
            return await base.DropId(p_ID);
        }

        public async Task<bool> Deaktivate(Uzivatel p_Uzivatel)
        {
            p_Uzivatel.Aktivni = '0';
            await Update(p_Uzivatel);
            return true;
        }

        public override List<Uzivatel> GetDTOList(DataTable p_DataTable)
        {
            var output = new List<Uzivatel>();
            foreach (DataRow row in p_DataTable.Rows)
                output.Add(GetDTO(row));

            return output;
        }

        #region AT.NET Project

        public async Task<Uzivatel> SelectNormPrezdivka(string p_NormPrezdivka)
        {
            var start = DateTime.Now;
            Uzivatel output = new Uzivatel();
            var command = new OracleCommand("", Connection);
            command.CommandText = _SQL_SELECT_NP;
            command.Parameters.Add(":NormPrezdivka", p_NormPrezdivka);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    output = null;
                else
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    output = GetDTOList(dataTable)[0];
                }
            }
            return output;
        }

        public async Task<bool> InsertProc(Uzivatel p_Uzivatel)
        {
            string text = "call InsertUzivatel(:Prezdivka,:Jmeno,:Prijmeni,:Email,:HesloHash,:NormPrezdivka)";
            OracleCommand command = new OracleCommand(text, Connection);
            command.Parameters.Add(":Prezdivka", p_Uzivatel.Prezdivka);
            command.Parameters.Add(":Jmeno", p_Uzivatel.Jmeno);
            command.Parameters.Add(":Prijmeni", p_Uzivatel.Prijmeni);
            command.Parameters.Add(":Email", p_Uzivatel.Email);
            command.Parameters.Add(":HesloHash", p_Uzivatel.HesloHash);
            command.Parameters.Add(":NormPrezdivka", p_Uzivatel.NormPrezdivka);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        #endregion AT.NET Project

        public override void AddParameters(Dictionary<string, object> p_Parameters, Uzivatel p_Uzivatel, bool p_UseID = true)
        {
            p_Parameters.Add(":Jmeno", p_Uzivatel.Jmeno);
            p_Parameters.Add(":Prijmeni", p_Uzivatel.Prijmeni);
            p_Parameters.Add(":Email", p_Uzivatel.Email);
            if (p_UseID)
                p_Parameters.Add(":ID", p_Uzivatel.IDUzivatel);
        }

        public override void AddParametersID(Dictionary<string, object> p_Parameters, int p_ID)
        {
            p_Parameters.Add(":ID", p_ID);
        }

        public override int GetSeqValue(DataTable p_DataTable)
        {
            return Convert.ToInt32(((DataRow)p_DataTable.Rows[0])["value"]);
        }

        public override Uzivatel GetDTO(DataRow p_DataRow)
        {
            return new Uzivatel
            {
                IDUzivatel = Convert.ToInt32(p_DataRow["uz_id"]),
                Prezdivka = Convert.ToString(p_DataRow["uz_prezdivka"]),
                Jmeno = Convert.ToString(p_DataRow["uz_jmeno"]),
                Prijmeni = Convert.ToString(p_DataRow["uz_prijmeni"]),
                Email = Convert.ToString(p_DataRow["uz_email"]),
                DatumRegistrace = Convert.ToDateTime(p_DataRow["uz_datumregistrace"]),
                PocetPodukolu = Convert.ToInt32(p_DataRow["uz_pocetpodukolu"]),
                Aktivni = Convert.ToChar(p_DataRow["uz_aktivni"]),
                NormPrezdivka = Convert.ToString(p_DataRow["uz_normprezdivka"]),
                HesloHash = Convert.ToString(p_DataRow["uz_heslohash"])
            };
        }
    }
}
