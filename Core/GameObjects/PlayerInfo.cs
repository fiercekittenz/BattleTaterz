using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.GameObjects
{
   /// <summary>
   /// Stub of information on the player that can be leveraged by the various scenes for
   /// display.
   /// </summary>
   public class PlayerInfo
   {
      public Guid Id { get; set; } = Guid.NewGuid();

      public string Name { get; set; } = "Player";

      public PlayerInfo(string name)
      {
         Name = name;
      }
   }
}
