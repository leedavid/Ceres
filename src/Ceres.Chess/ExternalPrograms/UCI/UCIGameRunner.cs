#region License notice

/*
  This file is part of the Ceres project at https://github.com/dje-dev/ceres.
  Copyright (C) 2020- by David Elliott and the Ceres Authors.

  Ceres is free software under the terms of the GNU General Public License v3.0.
  You should have received a copy of the GNU General Public License
  along with Ceres. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Ceres.Chess.External.CEngine;

#endregion

namespace Ceres.Chess.ExternalPrograms.UCI
{
  public enum UCIMoveLimitType
  {
    NodeCount,
    TimeMove,
    TimeGameForWhiteAndBlack
  }

  /// <summary>
  /// Manages execution of an external UCI engine as a separate process,
  /// controled by standard input and output.
  /// </summary>
  public class UCIGameRunner
  {
    public static bool UCI_VERBOSE_LOGGING = false;
    public readonly int Index;

    protected UCIEngineProcess engine;

    public readonly string EngineEXE;

    public readonly string EngineExtraCommand;

    public readonly bool ResetStateAndCachesBeforeMoves;

    public bool IsShutdown => engine == null;

    protected string curPosAndMoves = null;

    protected int doneCount = 0;
    protected double startTime;
    protected double freq;

    public int EngineProcessID => engine.EngineProcess.Id;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="exe"></param>
    /// <param name="extraCommand"></param>
    /// <param name="readHandler"></param>
    /// <param name="numThreads">N.B. when doing single static position evals from LC0, must set to 1</param>
    /// <returns></returns>
    UCIEngineProcess StartEngine(string engineName, string exePath, string extraCommand, ReadEvent readHandler, int numThreads = 1)
    {
      UCIEngineProcess engine = new UCIEngineProcess(engineName, exePath, extraCommand);
      engine.ReadEvent += readHandler;
      engine.StartEngine();

      engine.ReadAsync();

      engine.SendCommandLine("uci");
      engine.SendIsReadyAndWaitForOK();
     
      return engine;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="engineEXE"></param>
    /// <param name="engineExtraCommand"></param>
    /// <param name="runnerIndex">optional index of this runner within parallel set of runners</param>
    public UCIGameRunner(string engineEXE,
                          bool resetStateAndCachesBeforeMoves,
                          string extraCommandLineArguments = null, 
                          string[] uciSetOptionCommands = null, 
                          int runnerIndex = -1)
    {
      EngineEXE = engineEXE;
      ResetStateAndCachesBeforeMoves = resetStateAndCachesBeforeMoves;
      EngineExtraCommand = extraCommandLineArguments;
      Index = runnerIndex;

      ReadEvent readHandler = new ReadEvent(DataRead);

      string engine1Name = new FileInfo(engineEXE).Name;
      engine = StartEngine(engine1Name, engineEXE, extraCommandLineArguments, readHandler);

      System.Threading.Thread.Sleep(20);

      if (uciSetOptionCommands != null)
      {
        foreach (string extraCommand in uciSetOptionCommands)
          engine.SendCommandLine(extraCommand);
      }

      freq = Stopwatch.Frequency;
      startTime = Stopwatch.GetTimestamp();
    }


    protected volatile string lastInfo;
    protected volatile string lastBestMove = null;
    protected volatile string lastError = null;

    public string LastInfoString => lastInfo;
    public string LastBestMove => lastBestMove;

    protected volatile UCISearchInfo lastSearchInfo;

    protected UCISearchInfo engine1LastSearchInfo;


    public List<string> InfoStringDict0 = new List<string>();

    void DataRead(int id, string data)
    {
      double elapsedTime = (double)(Stopwatch.GetTimestamp() - startTime) / freq;
      if (UCI_VERBOSE_LOGGING) Console.WriteLine(Math.Round(elapsedTime, 3) + " ENGINE::{0}::{1}", id, data);

      if (data.Contains("error"))
        lastError = data;
      else if (data.Contains("bestmove"))
        lastBestMove = data;
      else if (data.Contains("info string"))
      {
        InfoStringDict0.Add(data);
      }
      else if (data.Contains("info"))
      {
        if (lastSearchInfo == null || data.Contains("score")) // ignore things like "info time" because it might not contain score info and overwrite prior good info
        {
          lastSearchInfo = new UCISearchInfo(data, lastBestMove, InfoStringDict0);// id == 0 ? InfoStringDict0 : null);
          lastInfo = data;
        }
      }
    }


    public UCISearchInfo EvalPositionToMovetime(string fen, string movesString, int moveTimeMilliseconds)
    {
      return EvalPosition(fen, movesString, "movetime", moveTimeMilliseconds);
    }

    public UCISearchInfo EvalPositionRemainingTime(string fen,
                                                   string movesString,
                                                   bool whiteToMove,
                                                   int? movesToGo,
                                                   int remainingTimeMilliseconds, 
                                                   int incrementTimeMilliseconds)
    {
      string prefixChar = whiteToMove ? "w" : "b";
      string moveStr = $"go {prefixChar}time {Math.Max(1, remainingTimeMilliseconds)}";
      if (incrementTimeMilliseconds > 0) moveStr += $" {prefixChar}inc {incrementTimeMilliseconds}";
      if (movesToGo.HasValue) moveStr += " movestogo " + movesToGo.Value;
      return EvalPosition(fen, movesString, null, 0, moveStr);
    }


    public UCISearchInfo EvalPositionToNodes(string fen, string movesString, int numNodes)
    {
      return EvalPosition(fen, movesString, "nodes", numNodes);
    }


    protected void SendCommandCRLF(UCIEngineProcess thisEngine, string cmd)
    {
      if (UCI_VERBOSE_LOGGING) Console.WriteLine("--> CMD " + cmd);
      thisEngine.SendCommandLine(cmd);
    }


    bool havePrepared = false;


    /// <summary>
    /// Executes any preparatory UCI commands before sending a position for evaluation.
    /// These preparatory steps are typically not counted in the search time for the engine.
    /// </summary>
    /// <param name="engineNum"></param>
    public void EvalPositionPrepare()
    {
      if (ResetStateAndCachesBeforeMoves)
      {
        // Not all engines support Clear hash, e.g.
        // "option name Clear Hash type button"
        // so we do not issue this command.
        //thisEngine.SendCommandLine("setoption name Clear Hash");

        // Perhaps ucinewgame helps reset state
        engine.SendCommandLine("ucinewgame");
        engine.SendIsReadyAndWaitForOK();
      }

      havePrepared = true;
    }


    public void StartNewGame()
    {
      SendCommandCRLF(engine, "ucinewgame");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="fen"></param>
    /// <param name="movesString"></param>
    /// <param name="moveType"></param>
    /// <param name="moveMetric"></param>
    /// <param name="moveOverrideString"></param>
    /// <returns></returns>
    public UCISearchInfo EvalPosition(string fen, string movesString, 
                                      string moveType, int moveMetric, 
                                      string moveOverrideString = null)
    {
      if (!havePrepared) throw new Exception("UCIGameRunner.EvalPositionPrepare should be called each time before EvalPosition.");

     
      lastBestMove = null;
      lastInfo = null;

      string curPosCmd = "position fen " + fen;
      if (movesString != null && movesString != "") curPosCmd += " moves " + movesString;
      SendCommandCRLF(engine, curPosCmd);

      if (moveOverrideString != null)
        SendCommandCRLF(engine, moveOverrideString);
      else
        SendCommandCRLF(engine, "go " + moveType + " " + moveMetric);

      string desc = $"{curPosCmd} on {EngineEXE} {EngineExtraCommand}";


      int waitCount = 0;
      while (lastBestMove == null || !lastBestMove.Contains("bestmove"))
      {
        if (engine.EngineProcess.HasExited)
          throw new Exception($"The engine process has exited: {desc}");
        else if (lastError != null)
          throw new Exception($"UCI error {desc} : {lastError}");

        System.Threading.Thread.Sleep(1);
        if ((waitCount == 5000 || waitCount == 9000) && moveType == "nodes" && moveMetric <= 1000)
          Console.WriteLine($"--------------> Warn: waiting >{waitCount}ms for {desc}");
        waitCount++;
      }

      havePrepared = false;
      if (lastBestMove != null)
      {
        UCISearchInfo ret = new UCISearchInfo(lastInfo, lastBestMove,InfoStringDict0);
        return ret;
      }
      else
        return null;
    }


    public void Shutdown()
    {
      engine.SendCommandLine("stop");
      engine.SendCommandLine("quit");
      engine.Shutdown();
      engine = null;
    }

  }

}
