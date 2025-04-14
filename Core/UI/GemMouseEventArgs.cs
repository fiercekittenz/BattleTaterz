using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.UI
{
   /// <summary>
   /// Events bubbled up from a gem when there is a mouse event.
   /// </summary>
   public class GemMouseEventArgs : EventArgs
   {
      public enum MouseEventType
      {
         None,
         Enter,
         Leave,
         Click
      };

      public MouseEventType EventType { get; set; } = MouseEventType.None;
   }
}
