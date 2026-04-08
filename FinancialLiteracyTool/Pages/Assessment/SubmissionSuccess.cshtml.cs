using FinancialLiteracyTool.Model.Questions;
using FinancialLiteracyTool.MyAppHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace FinancialLiteracyTool.Pages.Assessment
{
    public class SubmissionSuccessModel : PageModel
    {
        // Reads from the Json and stores user answers as dictionary
        // Key = QuestionID, value = ChoiceID
        Dictionary<string, string> Answers { get; set; } = new Dictionary<string, string>();
        public List<int> AnswerIDs { get; set; } = new List<int>();
        public int CorrectAnswers { get; set; }
        public float Score { get; set; }

        public void OnGet()
        {
            TempData.Keep("AnswersJson");

            var json = TempData["AnswersJson"] as string;

            Answers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

            string IDs = IDsToString(Answers);
            CountCorrectAnswers(IDs);
            Score = (float)Math.Round(((float)CorrectAnswers / Answers.Count) * 100, 2);
        }// End of ''.

        private void CountCorrectAnswers(string IDs)
        {
            //int i = 0;
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT IsCorrect FROM QuestionChoices WHERE ChoiceID IN " + IDs;
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        if (reader.GetBoolean(0) == true)
                        {
                            CorrectAnswers++;
                        }
                    }
                }
            }
        }// End of ''.

        private string IDsToString(Dictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("(");

            foreach (var kv in dict)
            {
                sb.Append($"'{kv.Value}', ");
            }

            sb.Length -= 2; // Remove the last ", "

            sb.Append(")");

            string result = sb.ToString();

            return result;
        }// End of 'IDsToString'.
    }// End of ''.
}// End of ''.
