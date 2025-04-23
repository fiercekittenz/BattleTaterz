using BattleTaterz.Core.Enums;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BattleTaterz.Core.Utility
{
   #region Public Methods

   /// <summary>
   /// Static class that handles logging of the game board and other game events.
   /// </summary>
   public class DebugLogger
   {
      // Singleton Pattern
      public static DebugLogger Instance
      {
         get
         {
            if (_instance == null)
            {
               _instance = new DebugLogger();
            }

            return _instance;
         }
      }

      /// <summary>
      /// The level at which an entry should be indented.
      /// </summary>
      public int IndentLevel { get; set; } = 0;

      /// <summary>
      /// Is the logging system even enabled?
      /// </summary>
      public bool Enabled { get; set; } = false;

      /// <summary>
      /// The logging level that needs to match in order for the log request to be output.
      /// </summary>
      public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

      /// <summary>
      /// Basic log function. Logs whatever text is provided.
      /// </summary>
      /// <param name="text"></param>
      /// <param name="logLevel"></param>
      public void Log(string text, LogLevel logLevel)
      {
         if (_enabled && LoggingLevel <= logLevel)
         {
            using (StreamWriter stream = new StreamWriter(_logFile, true))
            {
               string indentation = GetIndentPrefix();
               stream.WriteLine($"[{DateTime.Now.ToString("MM-dd-yyyy H:mm:ss")}][{logLevel.ToString()}] {indentation}{text}");
            }
         }
      }

      /// <summary>
      /// Log method that will log each individual line after a headline. Good for logging chunks of information.
      /// </summary>
      /// <param name="headline"></param>
      /// <param name="lines"></param>
      /// <param name="logLevel"></param>
      public void LogLines(string headline, List<string> lines, LogLevel logLevel)
      {
         if (_enabled && LoggingLevel <= logLevel)
         {
            using (StreamWriter stream = new StreamWriter(_logFile, true))
            {
               string indentation = GetIndentPrefix();
               stream.WriteLine($"[{DateTime.Now.ToString("MM-dd-yyyy H:mm:ss")}][{logLevel.ToString()}] {indentation}{headline}");
               foreach (var line in lines)
               {
                  stream.WriteLine($"{indentation}{line}");
               }
            }
         }
      }

      /// <summary>
      /// Logs the state of the game board.
      /// </summary>
      /// <param name="headline"></param>
      /// <param name="tileCount"></param>
      /// <param name="gameBoard"></param>
      /// <param name="logLevel"></param>
      public void LogGameBoard(string headline, int tileCount, ref Tile[,] gameBoard, LogLevel logLevel)
      {
         if (_enabled && LoggingLevel <= logLevel)
         {
            List<string> gems = new List<string>();
            List<string> positions = new List<string>();

            for (int row = 0; row < tileCount; ++row)
            {
               string gemText = string.Empty;
               string positionText = string.Empty;

               for (int column = 0; column < tileCount; ++column)
               {
                  string gemValue = "*"; // assume null until proven otherwise
                  string positionValue = "";

                  if (gameBoard[row, column] != null)
                  {
                     gemValue = $"{(int)gameBoard[row, column].CurrentGemType}";
                     positionValue = $"({gameBoard[row, column].Position.X}, {gameBoard[row, column].Position.Y})";
                  }

                  gemText = $"{gemText} {gemValue}";
                  positionText = $"{positionText}   {positionValue}";
               }

               gems.Add(gemText);
               positions.Add(positionText);
            }

            LogLines(headline, gems, logLevel);
            LogLines(headline, positions, logLevel);
         }
      }

      #endregion

      #region Private Methods

      /// <summary>
      /// Constructor
      /// </summary>
      private DebugLogger()
      {
         Directory.CreateDirectory("logs");
         _logFile = $"logs/battletaterz_debug_log_{DateTime.Now.ToString("MM_dd_yyyy_H_mm_ss")}.txt";
      }

      /// <summary>
      /// Returns the current indention level in string format.
      /// </summary>
      /// <returns></returns>
      private string GetIndentPrefix()
      {
         string indentPrefix = string.Empty;
         for (int indent = 0; indent < IndentLevel; ++indent)
         {
            indentPrefix = $"{indentPrefix}\t";
         }

         return indentPrefix;
      }

      #endregion

      #region Private Members

      private static DebugLogger _instance;

      private string _logFile;

      private bool _enabled = true;

      #endregion
   }
}
