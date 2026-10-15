using System;
using System.Collections.Generic;

// -----------------------
// Not for you to edit. Part of the debugger demo (README_Debugger.txt).
// Station 5 : a recursive function, and what the call stack looks like when the recursion never ends.
// -----------------------

public class DbgFleetNode
{
    public readonly string Name;
    public readonly int Crew;
    public readonly List<DbgFleetNode> Children = new List<DbgFleetNode>();

    public DbgFleetNode(string name, int crew)
    {
        Name = name;
        Crew = crew;
    }

    public DbgFleetNode Add(DbgFleetNode child)
    {
        Children.Add(child);
        return child;
    }

    // A real StackOverflowException would kill the Unity editor, so the recursion is guarded.
    public int CountCrew(int depth)
    {
        if (depth > 40)
        {
            throw new InvalidOperationException("Fleet tree is too deep (cycle ?) at " + Name);
        }

        int total = Crew;                       // [BP-5] conditional breakpoint : depth == 2

        foreach (DbgFleetNode child in Children)
        {
            total += child.CountCrew(depth + 1);
        }

        return total;
    }
}
