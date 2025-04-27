using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   /// <summary>
   /// Blueprint for the creation of tile behaviors.
   /// </summary>
   public abstract class TileBehavior
   {
      /// <summary>
      /// Triggers the behavior.
      /// </summary>
      /// <param name="tileOwner">The game board that holds the tile with this assigned behavior.</param>
      /// <param name="matchDetails">The match that led to the behavior being triggered.</param>
      /// <param name="matchPoints">The match's score before applying special behaviors.</param>
      /// <returns>TriggerResult object with details on the outcome.</returns>
      public TriggerResult Trigger(GameBoard tileOwner, MatchDetails matchDetails, int matchPoints)
      {
         TriggerResult result = InternalTrigger(tileOwner, matchDetails, matchPoints);
         return result;
      }

      /// <summary>
      /// Public property to access the graphic for this behavior.
      /// </summary>
      public abstract TileBorder Graphic { get; }

      /// <summary>
      /// Internal method called from the public Trigger() method. Functionality pertaining to
      /// the results of the behavior should be defined in this method.
      /// </summary>
      /// <param name="tileOwner">The game board that holds the tile with this assigned behavior.</param>
      /// <param name="matchDetails">The match that led to the behavior being triggered.</param>
      /// <param name="matchPoints">The match's score before applying special behaviors.</param>
      /// <returns>TriggerResult object with details on the outcome.</returns>
      protected abstract TriggerResult InternalTrigger(GameBoard tileOwner, MatchDetails matchDetails, int matchPoints);
   }
}
