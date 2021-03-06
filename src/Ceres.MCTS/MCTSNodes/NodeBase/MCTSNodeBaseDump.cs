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

using Ceres.Chess;
using Ceres.Base.DataType.Trees;
using Ceres.Chess.MoveGen;
using Ceres.Chess.MoveGen.Converters;

#endregion

namespace Ceres.MCTS.MTCSNodes
{
  /// <summary>
  /// Utility methods for dumping information relating to the MCTS node,
  /// including ToString() for a single node and Dump to dump statistics
  /// relating to moves from root.
  /// </summary>
  public partial class MCTSNode : ITreeNode, IEqualityComparer<MCTSNode>
  {
    public override string ToString()
    {
      string pendingWDL = "";
      if (!EvalResult.IsNull) pendingWDL = $" PendingWDL={EvalResult.WinP:F3}/{EvalResult.DrawP:F3}/{EvalResult.LossP:F3}";

     
      string priorMoveStr = Annotation.PriorMoveMG.ToString();

      return $"<MCTSNode [#{Index}] Depth {Depth} {priorMoveStr} [{ActionType}]  ({N},{NInFlight},{NInFlight2})  [{P:F3}%] {Q:F3} "
           + $"Parent={(ParentIndex.IsNull ? "none" : ParentIndex.Index.ToString())}"
           + $" V={V:F3} " + (VSecondary == 0 ? "" : $"VSecondary={VSecondary:F3} ")
           + pendingWDL
           + $" MPos={MPosition:F3} MAvg={MAvg:F3} "
           + $" Score0={(IsRoot ? float.NaN : Parent.ChildScore(0, Depth, IndexInParentsChildren)),6:F3} "
           + $" Score1={(IsRoot ? float.NaN : Parent.ChildScore(1, Depth, IndexInParentsChildren)),6:F3} "
           + $" with {NumPolicyMoves} policy moves>";
    }


    /// <summary>
    /// Dumps detailed information about possible moves from this node
    /// and optionally from descendent nodes up to a specified depth.
    /// </summary>
    /// <param name="lastLevel"></param>
    /// <param name="firstLevelStartPVOnly"></param>
    /// <param name="minNodes"></param>
    /// <param name="prefixString"></param>
    /// <param name="shouldAbort"></param>
    public void Dump(int lastLevel = int.MaxValue,
                     int firstLevelStartPVOnly = int.MaxValue,
                     int minNodes = int.MinValue,
                     string prefixString = null,
                     Predicate<MCTSNode> shouldAbort = null,
                     int maxMoves = int.MaxValue)
    {
      if (N < minNodes) return;

      if (shouldAbort != null && shouldAbort(this))
        return;

      Position cPos = MGChessPositionConverter.PositionFromMGChessPosition(Annotation.PosMG);

      float multiplerOurPerspective = Annotation.PosMG.BlackToMove ? -1.0f : 1.0f;

      bool minimize = true;
      float bestChildValue = minimize ? int.MaxValue : int.MinValue;
      if (Parent != null)
      {
        MCTSNode[] sortedChildren = Parent.ChildrenSorted(n => n.V);
        bestChildValue = minimize ? sortedChildren[0].V : sortedChildren[^1].V;
      }

      // Print extra characters for nodes with special characteristics
      char extraFlag = ' ';
      if (!IsRoot && parent.IsRoot && Context.RootMovesArePruned != null && Context.RootMovesArePruned[IndexInParentsChildren])
        extraFlag = 'S'; // move has been shutdown from further leaf expansion due to futility pruning
      else if (Terminal == GameResult.Draw)
        extraFlag = 'D';
      else if (Terminal == GameResult.Checkmate)
        extraFlag = 'C';
      else if (IsTranspositionLinked)
        extraFlag = 'T';

      double u = float.NaN;
      // not yet implemented      if (!IsRoot) u = Parent.U(Context.ParamsSearch.Execution.FLOW_DUAL_SELECTORS, Context.ParamsSelect, 0, this.IndexInParentsChildren);
      string extraInfo = prefixString;
      float sideMult = Depth % 1 == 0 ? -1.0f : 1.0f;

//      extraInfo = $" N={N,9:F0}  Q={sideMult * Q,6:F3}   P={P * 100,6:F2}%  V={ multiplerOurPerspective * V,6:F3} ";
      extraInfo = $" N={N,9:F0}  Q={sideMult * Q,6:F3}  V={ multiplerOurPerspective * V,6:F3} ";
      extraInfo += $" WDL= {WinP,4:F2} {DrawP,4:F2} {LossP,4:F2} ";
      extraInfo += $" WDL Avg= {WAvg,4:F2} {DAvg,4:F2} {LAvg,4:F2}  ";
      extraInfo += $"M = {MPosition,4:F0} {MAvg,4:F0}  ";

      //float qStdDev = MathF.Sqrt(Ref.VVariance);
      //extraInfo += $"Sc={sideMult * Q + u,5:F3}  ";//
      //extraInfo+= $"U={u,5:F3} {cPos.FEN}";

      MGMove move = Annotation.PriorMoveMG;
      Console.WriteLine(
                        $"{extraFlag} {Depth,4:F0} {move.MoveStr(MGMoveNotationStyle.ShortAlgebraic),-6} {100 * P,8:F2}% " +
                        extraInfo
                        );
      
      if (Depth < lastLevel && ExpandedChildrenList.Count > 0)
      {
        if (Depth >= firstLevelStartPVOnly)
        {
          MCTSNode[] sortedChildren2 = ChildrenSorted(n => -n.N);
          sortedChildren2[0].Dump(firstLevelStartPVOnly, lastLevel, minNodes, maxMoves:maxMoves);

        }
        else
        {
          // Make a copy of children and sort as desired 
          MCTSNode[] sortedChildren1 = ChildrenSorted(n => -n.N);
          string otherNodesMessage = null;
          if (sortedChildren1.Length > maxMoves)
          {
            otherNodesMessage = $"  (followed by {sortedChildren1.Length - maxMoves} additional moves not shown...)";
            Array.Resize(ref sortedChildren1, maxMoves);
          }

          foreach (MCTSNode child in sortedChildren1)
            child.Dump(lastLevel, firstLevelStartPVOnly, minNodes, maxMoves: maxMoves);
          if (otherNodesMessage != null) Console.WriteLine(otherNodesMessage);
          Console.WriteLine();
        }
      }
    }

  }


}
