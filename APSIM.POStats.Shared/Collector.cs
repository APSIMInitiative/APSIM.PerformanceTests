using APSIM.POStats.Shared.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace APSIM.POStats.Shared
{
    public class Collector
    {
        /// <summary>
        /// Retrieves all Apsimx simulation files with for the search directory specified in the App.config file
        /// and then process these files.
        /// 
        /// Returns true iff an error is encountered.
        /// </summary>
        /// <remarks>
        /// To get this collector to work when sending > 30Mb of data to api I had to change IIS setting:
        /// In configuration editor on APSIM web site:
        /// system->webServer->security->requestFiltering->maxAllowedContentLength = 60000000
        /// </remarks>
        /// 
        /// <param name="pullId"></param>
        /// <param name="runDate"></param>
        /// <param name="submitDetails"></param>
        /// <param name="filePaths">A collection of file paths to search.</param>
        public static PullRequest RetrieveData(int pullId, DateTime runDate, string submitDetails, IEnumerable<string> filePaths)
        {
            var pullRequest = new PullRequest()
            {
                Number = pullId,
                Author = submitDetails,
                DateRun = runDate,
                Files = new List<ApsimFile>()
            };

            string errorMessages = string.Empty;
            foreach (var filePath in filePaths)
            {
                string currentPath = filePath.Trim();
                DirectoryInfo info = new DirectoryInfo(@currentPath);
                foreach (FileInfo fileInfo in info.GetFiles("*.apsimx", SearchOption.AllDirectories))
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        var apsimFile = new ApsimFile()
                        {
                            Name = Path.GetFileNameWithoutExtension(fileInfo.FullName),
                            PullRequest = pullRequest,
                            PullRequestId = pullRequest.Id,
                            Tables = GetTablesFromFile(fileInfo.FullName)
                        };
                        if (apsimFile.Tables.Count > 0)
                            pullRequest.Files.Add(apsimFile);
                        Console.WriteLine($"Read PO data from {fileInfo.FullName} in {stopwatch.Elapsed.Seconds} second(s).");
                    }
                    catch (Exception ex)
                    {
                        errorMessages += ex.ToString();
                    }
                }
            }
            if (errorMessages.Length > 0)
                throw new Exception(errorMessages);

            return pullRequest;
        }

        /// <summary>
        /// Gets all predicted / observed tables in the .db file associated with a .apsimx file.
        /// </summary>
        /// <param name="apsimxFileName">The .apsimx file name.</param>
        private static List<Table> GetTablesFromFile(string apsimxFileName)
        {
            var tables = new List<Table>();
            var databasePath = Path.ChangeExtension(apsimxFileName, ".db");
            if (File.Exists(databasePath))
            {
                using (SqliteConnection db = new SqliteConnection($"Data Source={databasePath}"))
                {
                    db.Open();

                    // Get the simulation table so that we can later match simulation ids to names.
                    var simulationIdNamePairs = GetSimulationNameIdPairs(db);

                    foreach (var tableName in SqliteUtilities.GetTableNames(db))
                    {
                        var columns = SqliteUtilities.GetColumnNames(db, tableName);

                        var predictedColumnNames = columns.Where(c => c.StartsWith("Predicted."))
                                                          .Select(c => c.Replace("Predicted.", ""));
                        var observedColumnNames = columns.Where(c => c.StartsWith("Observed."))
                                                         .Select(c => c.Replace("Observed.", ""));

                        var columnNames = predictedColumnNames.Intersect(observedColumnNames);

                        if (columnNames.Any())
                        {
                            var newTable = new Table
                            {
                                Name = tableName,
                                Variables = new List<Variable>()
                            };
                            foreach (var columName in columnNames)
                            {
                                newTable.Variables.Add(new Variable()
                                {
                                    Name = columName,
                                    Data = new List<VariableData>()
                                });
                            }

                            var matchFields = GetMatchColumnNamesForTable(apsimxFileName, tableName)
                                              .Where(f => f != "SimulationName");

                            string selectSQL = $"SELECT * FROM {tableName}";
                            using (SqliteCommand cmd = new SqliteCommand(selectSQL, db))
                            {
                                using (SqliteDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreach (var columnName in columnNames)
                                        {
                                            var predictedValue = reader[$"Predicted.{columnName}"];
                                            var observedValue = reader[$"Observed.{columnName}"];

                                            // If the predicted or observed value is an integer, convert to double.
                                            if (predictedValue is int || predictedValue is Int64)
                                                predictedValue = Convert.ToDouble(predictedValue);
                                            if (observedValue is int || observedValue is Int64)
                                                observedValue = Convert.ToDouble(observedValue);
                                            
                                            if (predictedValue is double)
                                            {
                                                double observedValueAsDouble = double.NaN;
                                                if (observedValue is double)
                                                    observedValueAsDouble = (double)observedValue;
                                                else if (observedValue is string)
                                                    if (!double.TryParse(observedValue.ToString(), out observedValueAsDouble))
                                                        observedValueAsDouble = double.NaN;

                                                // Only add data point if observed value is now a double.
                                                if (!double.IsNaN(observedValueAsDouble))
                                                {
                                                    var data = new VariableData()
                                                    {
                                                        Predicted = Convert.ToDouble(predictedValue),
                                                        Observed = observedValueAsDouble,
                                                        Label = CreateLabel(reader, matchFields, simulationIdNamePairs)
                                                    };
                                                    newTable.Variables.Find(v => v.Name == columnName)
                                                            .Data.Add(data);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Remove variables that have no data.
                            newTable.Variables.RemoveAll(v => v.Data.Count == 0);

                            // Calculate stats.
                            foreach (var variable in newTable.Variables)
                                VariableFunctions.EnsureStatsAreCalculated(variable);

                            // Only add the table to the return list if it has variables.
                            if (newTable.Variables.Count > 0)
                                tables.Add(newTable);
                        }
                    }
                    db.Close();
                }
            }
            return tables;
        }

        /// <summary>
        /// Get a simulation id to name dictionary from a .db file.
        /// </summary>
        /// <param name="db">The db to read.</param>
        private static Dictionary<long, string> GetSimulationNameIdPairs(SqliteConnection db)
        {
            var returnPairs = new Dictionary<long, string>();

            using (SqliteCommand cmd = new SqliteCommand("SELECT * FROM [_Simulations]", db))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        returnPairs.Add((long)reader["ID"], (string)reader["Name"]);
                }
            }
            return returnPairs;
        }

        /// <summary>
        /// Get the column names that were used to match the predicted/observed data in a table.
        /// </summary>
        /// <param name="apsimxFileName">The .apsimx file that contains the match column names.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <returns></returns>
        private static IEnumerable<string> GetMatchColumnNamesForTable(string apsimxFileName, string tableName)
        {
            var options = new JsonDocumentOptions { AllowTrailingCommas = true };
            using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(apsimxFileName), options))
            {
                var rootNode = new JsonProxyForModel(document.RootElement);
                var predictedObservedModel = rootNode.ChildrenRecursively
                                            .FirstOrDefault(child => child.Type == "PredictedObserved" &&
                                                                     child.Name == tableName);
                if (predictedObservedModel == null)
                    throw new Exception($"Cannot find predicted observed table {tableName} in file {apsimxFileName}");

                var matchElements = new List<string>()
                {
                    predictedObservedModel.GetPropertyValue("FieldNameUsedForMatch"),
                    predictedObservedModel.GetPropertyValue("FieldName2UsedForMatch"),
                    predictedObservedModel.GetPropertyValue("FieldName3UsedForMatch")
                };
                return matchElements.Where(element => element != null);
            }
        }

        /// <summary>
        /// Create a label for the current row of a SqliteDataReader
        /// </summary>
        /// <param name="reader">The reader to examine.</param>
        /// <param name="matchFields">The field names to use to create the label.</param>
        /// <returns></returns>
        private static string CreateLabel(SqliteDataReader reader, IEnumerable<string> matchFields, Dictionary<long, string> simulationIdNamePairs)
        {
            // Get the simulation name for the readers current id.
            var simulationId = (long) reader["SimulationID"];
            if (!simulationIdNamePairs.TryGetValue(simulationId, out string simulationName))
                throw new Exception($"Cannot find a simulation name for id {simulationId}");

            // Create a label from the simulation name and the match fields.
            var label = new StringBuilder();
            label.Append("Simulation: ");
            label.Append(simulationName);
            foreach (var fieldName in matchFields)
            {
                if (label.Length > 0)
                    label.Append(", ");
                label.Append(fieldName);
                label.Append(": ");
                var fieldValue = reader[fieldName].ToString();
                if (DateTime.TryParse(fieldValue, out DateTime fieldValueAsDate))
                    fieldValue = fieldValueAsDate.ToString("yyyy-MM-dd");
                label.Append(fieldValue);
            }
            return label.ToString();
        }

        /// <summary>This class wraps a json element for an APSIM model.</summary>
        private class JsonProxyForModel
        {
            /// <summary>The json element.</summary>
            private readonly JsonElement element;

            /// <summary>Constructor.</summary>
            /// <param name="element">The json element to wrap.</param>
            public JsonProxyForModel(JsonElement jsonElement)
            {
                element = jsonElement;
            }

            /// <summary>The name of the APSiM model.</summary>
            public string Name
            {
                get
                {
                    var nameProperty = element.GetProperty("Name");
                    //if (nameProperty.ValueKind == JsonValueKind.Undefined)
                    //    return null;
                    return nameProperty.GetString();
                }
            }

            /// <summary>The type of the APSiM model.</summary>
            public string Type
            {
                get
                {
                    var typeName = element.GetProperty("$type");
                    if (typeName.ValueKind == JsonValueKind.Undefined)
                        return null;
                    var typeNameString = typeName.GetString();
                    int posComma = typeNameString.IndexOf(',');
                    if (posComma == -1)
                        return null;
                    typeNameString = typeNameString.Remove(posComma);
                    var typeNameStringWords = typeNameString.Split('.');
                    if (typeNameStringWords.Length == 0)
                        return null;

                    return typeNameStringWords.Last();
                }
            }

            /// <summary>The child models.</summary>
            public IEnumerable<JsonProxyForModel> Children
            {
                get
                {
                    if (element.TryGetProperty("Children", out JsonElement children))
                    {
                        foreach (var child in children.EnumerateArray())
                            yield return new JsonProxyForModel(child);
                    }
                }
            }

            /// <summary>The child models recursively.</summary>
            public IEnumerable<JsonProxyForModel> ChildrenRecursively
            {
                get
                {
                    if (element.TryGetProperty("Children", out JsonElement childrenElement))
                    {
                        foreach (var child in childrenElement.EnumerateArray())
                        {
                            var childProxyModel = new JsonProxyForModel(child);
                            yield return childProxyModel;
                            foreach (var nestedChild in childProxyModel.ChildrenRecursively)
                                yield return nestedChild;
                        }
                    }
                }
            }

            /// <summary>
            /// Get the value of a property.
            /// </summary>
            /// <param name="name">The name of the property to get.</param>
            public string GetPropertyValue(string name)
            {
                return element.GetProperty(name).GetString();
            }
        }
    }
}