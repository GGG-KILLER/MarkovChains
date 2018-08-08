using System;
using System.Collections.Generic;
using GUtils.Timing;

namespace MarkovConsole
{
    public class AutoStackingTA : TimingArea
    {
        private static readonly Stack<AutoStackingTA> stack = new Stack<AutoStackingTA> ( );

        public static void ClearStack ( )
        {
            while ( stack.Count > 0 )
            {
                stack.Pop ( ).Dispose ( );
            }
        }

        public AutoStackingTA ( String name ) : this ( name, stack.Count > 0 ? stack.Peek ( ) : null )
        {
        }

        public AutoStackingTA ( String name, TimingArea parent = null ) : base ( name, parent
            ?? ( stack.Count > 0 ? stack.Peek ( ) : null ) )
        {
            stack.Push ( this );
        }

        public new void Dispose ( )
        {
            this.Log ( $"Final timing: {  Timespans.Format ( this._stopwatch.ElapsedTicks, "{0:##00.00}{1}" )}", true );
            this.Log ( "}", false );
            while ( stack.Peek ( ) != this )
                stack.Pop ( );
            stack.Pop ( );
        }
    }
}
