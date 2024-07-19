using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Habit_Tracker
{
    internal class Program
    {
        static string connectionString = @"Data Source=habit-Tracker.db";

        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmdCreateHabitsTable = connection.CreateCommand();
                cmdCreateHabitsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Habits(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        HabitName TEXT NOT NULL,
                        Unit TEXT NOT NULL
                    )";
                cmdCreateHabitsTable.ExecuteNonQuery();
                connection.Close();
            }

            MainMenu();
        }

        private static void MainMenu()
        {
            Console.Clear();
            bool closeApp = false;

            while (!closeApp)
            {
                Console.WriteLine(@"Welcome to main menu
                0. Exit
                1. Create New Habit
                2. Manage Existing Habit
                ");
                string command = Console.ReadLine();

                switch (command)
                {
                    case "0":
                        Console.WriteLine("\n\nGoodbye!\n\n");
                        closeApp = true;
                        Environment.Exit(0);
                        break;
                    case "1":
                        CreateNewHabit();
                        break;
                    case "2":
                        ManageExistingHabit();
                        break;
                    default:
                        Console.WriteLine("Invalid input");
                        break;
                }
            }
        }

        private static void CreateNewHabit()
        {
            Console.Clear();
            Console.WriteLine("Enter the name of your new habit:");
            string habitName = Console.ReadLine();

            if (string.IsNullOrEmpty(habitName))
            {
                Console.WriteLine("Habit name cannot be empty. Please try again.");
                return;
            }

            Console.WriteLine("Enter the unit of your habit (e.g., glasses, minutes):");
            string unit = Console.ReadLine();

            if (string.IsNullOrEmpty(unit))
            {
                Console.WriteLine("Unit cannot be empty. Please try again.");
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmdInsertHabit = connection.CreateCommand();
                cmdInsertHabit.CommandText = $@"
                    INSERT INTO Habits (HabitName, Unit) VALUES ('{habitName}', '{unit}')
                ";
                cmdInsertHabit.ExecuteNonQuery();

                var cmdCreateHabitTable = connection.CreateCommand();
                cmdCreateHabitTable.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {habitName}(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        {unit} INTEGER NOT NULL
                    )
                ";
                cmdCreateHabitTable.ExecuteNonQuery();

                Console.WriteLine($"Habit '{habitName}' created successfully with unit '{unit}'.");
                connection.Close();
            }
        }

        private static void ManageExistingHabit()
        {
            Console.Clear();
            Console.WriteLine("Enter the name of the habit you want to manage:");
            string habitName = Console.ReadLine();

            if (string.IsNullOrEmpty(habitName))
            {
                Console.WriteLine("Habit name cannot be empty. Please try again.");
                return;
            }

            string unit;
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmdSelectUnit = connection.CreateCommand();
                cmdSelectUnit.CommandText = $@"SELECT Unit FROM Habits WHERE HabitName = '{habitName}'";
                unit = cmdSelectUnit.ExecuteScalar()?.ToString();

                if (string.IsNullOrEmpty(unit))
                {
                    Console.WriteLine("Habit not found. Please try again.");
                    connection.Close();
                    return;
                }

                connection.Close();
            }

            Console.Clear();
            bool closeApp = false;

            while (!closeApp)
            {
                Console.WriteLine($@"Managing Habit: {habitName}
                What would you like to do?
                Type 0 to Return to Main Menu.
                Type 1 to View All Records.
                Type 2 to Insert Record.
                Type 3 to Delete Record.
                Type 4 to Update Record.");
                Console.WriteLine("======================================\n");

                string command = Console.ReadLine();

                switch (command)
                {
                    case "0":
                        closeApp = true;
                        MainMenu();
                        break;
                    case "1":
                        GetAllRecords(habitName, unit);
                        break;
                    case "2":
                        InsertRecord(habitName, unit);
                        break;
                    case "3":
                        DeleteRecord(habitName, unit);
                        break;
                    case "4":
                        UpdateRecord(habitName, unit);
                        break;
                    default:
                        Console.Clear();
                        Console.WriteLine("Invalid option. Please type a number from 0 to 4.\n");
                        break;
                }
            }
        }

        private static void GetAllRecords(string habitName, string unit)
        {
            Console.Clear();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmdSelectAll = connection.CreateCommand();
                cmdSelectAll.CommandText = $"SELECT * FROM {habitName}";

                List<Habit> records = new List<Habit>();
                SqliteDataReader reader = cmdSelectAll.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        records.Add(new Habit
                        {
                            Id = reader.GetInt32(0),
                            Date = DateTime.ParseExact(reader.GetString(1), "dd-MM-yy", new CultureInfo("en-US")),
                            Unit = reader.GetInt32(2)
                        });
                    }
                }
                else
                {
                    Console.WriteLine("No records found.");
                }

                connection.Close();

                Console.WriteLine("======================================\n");
                foreach (var record in records)
                {
                    Console.WriteLine($"Id: {record.Id}\nDate: {record.Date:dd-MMM-yyyy}\n{unit}: {record.Unit}\n");
                }
                Console.WriteLine("======================================\n");
            }
        }

        private static void InsertRecord(string habitName, string unit)
        {
            string date = GetDateInput();
            int quantity = GetNumberInput($"\n\nPlease insert number of {unit}. (No decimals allowed)\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmdInsert = connection.CreateCommand();
                cmdInsert.CommandText =
                    $"INSERT INTO {habitName}(Date, {unit}) VALUES('{date}', '{quantity}')";

                cmdInsert.ExecuteNonQuery();
                connection.Close();
            }

            ManageExistingHabit();
        }

        private static void UpdateRecord(string habitName, string unit)
        {
            Console.Clear();
            GetAllRecords(habitName, unit);

            var recordId = GetNumberInput("\n\nPlease type Id of the record you would like to update. Type 0 to return to the main menu");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM {habitName} WHERE Id = {recordId})";
                int checkQuery = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (checkQuery == 0)
                {
                    Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist.\n\n");
                    connection.Close();
                    UpdateRecord(habitName, unit);
                }

                string date = GetDateInput();
                int quantity = GetNumberInput($"\n\nPlease insert number of {unit}. (No decimals allowed!)\n\n");

                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText =
                    $"UPDATE {habitName} SET Date = '{date}', {unit} = '{quantity}' WHERE Id = {recordId}";

                tableCmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        private static void DeleteRecord(string habitName, string unit)
        {
            Console.Clear();
            GetAllRecords(habitName, unit);

            var recordId = GetNumberInput("Please type the Id of the record you want to delete. Type 0 to return to the main menu");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText =
                    $"DELETE FROM {habitName} WHERE Id = {recordId}";

                int rowCount = tableCmd.ExecuteNonQuery();

                if (rowCount == 0)
                {
                    Console.WriteLine($"\n\nRecord with Id {recordId} does not exist.\n\n");
                    DeleteRecord(habitName, unit);
                }

                connection.Close();
            }

            Console.WriteLine($"\n\nRecord with Id {recordId} was deleted.\n\n");
            ManageExistingHabit();
        }

        internal static int GetNumberInput(string message)
        {
            Console.WriteLine(message);
            string numberInput = Console.ReadLine();

            while (!int.TryParse(numberInput, out _) || Convert.ToInt32(numberInput) < 0)
            {
                Console.WriteLine("\n\nInvalid number. Try again.\n\n");
                numberInput = Console.ReadLine();
            }

            return Convert.ToInt32(numberInput);
        }

        internal static string GetDateInput()
        {
            Console.WriteLine("\n\nPlease insert a date (Format: dd-MM-yy). Type 0 to return to the main menu!");
            string dateInput = Console.ReadLine();

            while (!DateTime.TryParseExact(dateInput, "dd-MM-yy", new CultureInfo("en-US"), DateTimeStyles.None, out _))
            {
                Console.WriteLine("\n\nInvalid date. (Format: dd-MM-yy). Type 0 to return to the main menu or try again:\n\n");
                dateInput = Console.ReadLine();
            }

            return dateInput;
        }
    }
}
