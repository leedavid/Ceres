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

using Ceres.MCTS.Evaluators;
using Ceres.MCTS.Iteration;

#endregion

namespace Ceres.MCTS.Search
{
  public partial class MCTSSearchFlow
  {
    public readonly MCTSManager Manager;
    public readonly MCTSIterator Context;

    public readonly MCTSNNEvaluator BlockNNEval1;
    public readonly MCTSNNEvaluator BlockNNEval2;

    /// <summary>
    /// Optional evaluator used as a secondary evaluator (experimental).
    /// </summary>
    public readonly MCTSNNEvaluator BlockNNEvalSecondaryNet;

    public readonly MCTSApply BlockApply;

    /// <summary>
    /// Object that manages preloading of nodes near the root 
    /// at beginning of search.
    /// </summary>
    MCTSRootPreloader rootPreloader;


    MCTSBatchParamsManager[] batchingManagers;

    public MCTSSearchFlow(MCTSManager manager, MCTSIterator context)
    {
      Manager = manager;
      Context = context;

      int numSelectors = context.ParamsSearch.Execution.FlowDirectOverlapped ? 2 : 1;
      batchingManagers = new MCTSBatchParamsManager[numSelectors];

      for (int i=0; i<numSelectors;i++)
        batchingManagers[i] = new MCTSBatchParamsManager(manager.Context.ParamsSelect.UseDynamicVLoss);

      bool shouldCache = context.EvaluatorDef.CacheMode != Chess.PositionEvalCaching.PositionEvalCache.CacheMode.None;

      string instanceID = "0";

      //Params.Evaluator1 : Params.Evaluator2;
      const bool LOW_PRIORITY_PRIMARY = false;
      LeafEvaluatorNN nodeEvaluator1 = new LeafEvaluatorNN(context.EvaluatorDef, context.NNEvaluators.Evaluator1, shouldCache,
                                                           LOW_PRIORITY_PRIMARY, context.Tree.PositionCache, null);// context.ParamsNN.DynamicNNSelectorFunc);
      BlockNNEval1 = new MCTSNNEvaluator(nodeEvaluator1, true);

      if (context.ParamsSearch.Execution.FlowDirectOverlapped)
      {
        // Create a second evaluator (configured like the first) on which to do overlapping.
        LeafEvaluatorNN nodeEvaluator2 = new LeafEvaluatorNN(context.EvaluatorDef, context.NNEvaluators.Evaluator2, shouldCache,
                                                             false, context.Tree.PositionCache, null);// context.ParamsNN.DynamicNNSelectorFunc);
        BlockNNEval2 = new MCTSNNEvaluator(nodeEvaluator2, true);
      }

      if (context.EvaluatorDef.SECONDARY_NETWORK_ID != null)
      {
        throw new NotImplementedException();
        //NodeEvaluatorNN nodeEvaluatorSecondary = new NodeEvaluatorNN(context.EvaluatorDef, context.ParamsNN.Evaluators.EvaluatorSecondary, false, false, null, null);
        //BlockNNEvalSecondaryNet = new MCTSNNEvaluate(nodeEvaluatorSecondary, false);
      }

      BlockApply = new MCTSApply(context.FirstMoveSampler);

      if (context.ParamsSearch.Execution.RootPreloadDepth > 0)
        rootPreloader = new MCTSRootPreloader();

      if (context.ParamsSearch.Execution.SmartSizeBatches)
      {
        context.NNEvaluators.CalcStatistics(true, 1f);
      }
    }

  }
}

