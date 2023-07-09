using CSSDotNet.Parser;

namespace CSSDotNet.Utils;

public class TokenStream : IDisposable
{
    private IEnumerator<Token> Enumerator { get; set; }
    private readonly Stack<int> _markedIndexes = new();
    // TODO Can this be more efficient? Once an unmarked token is discarded from the Enumerable, it is never used again.
    private List<Token?> Buffer { get; set; } = new();
    public int CurrentIndex { get; private set; } = -1;
    private Token? Current { get; set; }
    private Token? Next { get; set; }

    private int Position => CurrentIndex == -1 ? Buffer.Count : CurrentIndex;
    
    public TokenStream(IEnumerable<Token> enumerable)
    {
        Enumerator = enumerable.GetEnumerator();
    }

    public void Init()
    {
        if (Current != null) return;
        Current = Enumerator.MoveNext() ? Enumerator.Current : default;
        Next = Enumerator.MoveNext() ? Enumerator.Current : default;
        Buffer.Add(Current);
        Buffer.Add(Next);
    }
    
    public Token Peek()
    {
        if (CurrentIndex != -1 && CurrentIndex < Buffer.Count - 1)
        {
            return Buffer[CurrentIndex + 1]!;
        }
        
        if (Next == null) throw new InvalidOperationException("Next is null. This should never happen. You may have missed a check for the EOF token.");
        
        return Next;
    }

    public Token Read()
    {
        if (Current == null) throw new InvalidOperationException("You must call Init() before reading.");

        if (CurrentIndex != -1)
        {
            if (CurrentIndex == Buffer.Count - 1)
            {
                CurrentIndex = -1;
            }
            else
            {
                return Buffer[++CurrentIndex]!;
            }
        }
        Current = Next;
        var result = Current;
        Next = Enumerator.MoveNext() ? Enumerator.Current : default;
        if (Next != null)
            Buffer.Add(Next);
        return result!;
    }

    public void Mark()
    {
        _markedIndexes.Push(Position);
    }

    public void RestoreMark()
    {
        var index = _markedIndexes.Pop();
        CurrentIndex = index;
    }

    public void DiscardMark()
    {
        _markedIndexes.Pop();
    }

    public void Dispose()
    {
        Buffer.Clear();
        Enumerator.Dispose();
        GC.SuppressFinalize(this);
    }
}