﻿using System.Collections.Generic;
using Automation;
using Automation.ResultFiles;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Common.SQLResultLogging.InputLoggers
{
    public class ResultFileEntryLogger : DataSaverBase {
        private bool _isTableCreated;
        private const string TableName = "ResultFileEntries";
        public ResultFileEntryLogger([NotNull] SqlResultLoggingService srls): base(typeof(ResultFileEntry),
            new ResultTableDefinition(TableName,ResultTableID.ResultFileEntries, "Result files",CalcOption.BasicOverview),srls)
        {
            _isTableCreated = srls.CheckifTableExits(TableName);

            //_isTableCreated = isTableCreated;
        }

        public override void Run(HouseholdKey key, object o)
        {
            var hh = (ResultFileEntry)o;
            if (!_isTableCreated) {
                SaveableEntry se = GetStandardSaveableEntry(key);
                    se.AddRow(RowBuilder.Start("Name", Constants.GeneralHouseholdKey)
                        .Add("Json", JsonConvert.SerializeObject(hh, Formatting.Indented)).ToDictionary());
                    if (Srls == null)
                    {
                        throw new LPGException("Data Logger was null.");
                    }
                Srls.SaveResultEntry(se);
                _isTableCreated = true;
            }
            else {
                var row = RowBuilder.Start("Name", Constants.GeneralHouseholdKey)
                    .Add("Json", JsonConvert.SerializeObject(hh, Formatting.Indented)).ToDictionary();
                if (Srls == null)
                {
                    throw new LPGException("Data Logger was null.");
                }
                Srls.SaveDictionaryToDatabaseNewConnection(row, TableName, Constants.GeneralHouseholdKey);
            }
        }

        /// <summary>
        /// Deletes an entry from the result file list in the database
        /// </summary>
        /// <param name="rfe">The entry to delete</param>
        public void DeleteEntry(ResultFileEntry rfe)
        {
            if (!_isTableCreated)
            {
                throw new LPGException("Tried to delete entries from a table that did not exist: " + TableName);
            }
            // create a row as the one already in the database that should be deleted
            var row = RowBuilder.Start("Name", Constants.GeneralHouseholdKey)
                .Add("Json", JsonConvert.SerializeObject(rfe, Formatting.Indented)).ToDictionary();
            Srls.DeleteEntry(row, TableName, Constants.GeneralHouseholdKey);
        }

        [ItemNotNull]
        [NotNull]
        public List<ResultFileEntry> Load()
        {
            if (Srls == null)
            {
                throw new LPGException("Data Logger was null.");
            }
            return Srls.ReadFromJson<ResultFileEntry>(ResultTableDefinition, Constants.GeneralHouseholdKey,
                ExpectedResultCount.OneOrMore);
        }
    }
}
