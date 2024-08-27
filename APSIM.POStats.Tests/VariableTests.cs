using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;

namespace APSIM.POStats.Tests
{
    public class VariableTests
    {
        /// <summary>
        /// Make sure stats for a variable are calculated correctly.
        /// </summary>
        [Test]
        public void StatsAreCalculated()
        {
            Variable v = new Variable
            {
                Data = ToData(new List<(double, double)>
                {
                // predicted, observed
                    (11.0, 15.2),
                    (52.0, 1.7),
                    (11.5, 10.6)
                })
            };

            VariableFunctions.EnsureStatsAreCalculated(v);

            Assert.That(v.N, Is.EqualTo(3));
            Assert.That(v.NSE, Is.EqualTo(-26.0526).Within(0.0001));
            Assert.That(v.RMSE, Is.EqualTo(29.1464).Within(0.0001));
            Assert.That(v.RSR, Is.EqualTo(4.2467).Within(0.0001));
        }

        /// <summary>
        /// Ensure predicted / observed data can be retrieved from a variable.
        /// </summary>
        [Test]
        public void PODataCanBeRetrieved()
        {
            Variable v = new Variable
            {
                Data = ToData(new List<(double, double)>
                {
                // predicted, observed
                    (11.0,   15.2),
                    (52.0,    1.7),
                    (11.5,   10.6)
                })
            };
            VariableFunctions.GetData(v, out double[] predicted, out double[] observed, out _);
            Assert.That(predicted, Is.EqualTo(new double[] { 11.0, 52.0, 11.5 }));
            Assert.That(observed, Is.EqualTo(new double[] { 15.2, 1.7, 10.6 }));
        }

        /// <summary>
        /// Convert a list of PO data into a list of VariableData objects.
        /// </summary>
        /// <param name="poData">The predicted / observed data.</param>
        /// <returns></returns>
        private static List<VariableData> ToData(List<(double, double)> poData)
        {
            var data = new List<VariableData>();
            foreach (var poValue in poData)
            {
                data.Add(new VariableData()
                {
                    Predicted = poValue.Item1,
                    Observed = poValue.Item2
                });
            }
            return data;
        }

        /// <summary>
        /// Make sure the GitHub status works.
        /// </summary>
        [Test]
        public void TestGitHubSetStatus()
        {
            //GitHub.SetStatus(5901, false);
        }
    }
}