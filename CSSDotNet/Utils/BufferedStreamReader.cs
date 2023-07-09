namespace CSSDotNet.Utils;

public class BufferedStreamReader : IDisposable
{
    public int Line { get; private set; }
    public int Column { get; private set; }
    private StreamReader Reader { get; set; }
    private int Next { get; set; } = -1;
    private int NextNext { get; set; } = -1;
    private int NextNextNext { get; set; } = -1;
    private int NextNextNextNext { get; set; } = -1;
    
    public BufferedStreamReader(Stream stream)
    {
        Reader = new(stream);
    }
    
    public BufferedStreamReader(StreamReader reader)
    {
        Reader = reader;
    }

    public void Init()
    {
        if (Next != -1) return;
        Next = PreProcessInput(Reader.Read());
        NextNext = PreProcessInput(Reader.Read());
        NextNextNext = PreProcessInput(Reader.Read());
        NextNextNextNext = PreProcessInput(Reader.Read());
    }

    public int Peek()
    {
        return Next;
    }

    public int PeekTwo()
    {
        return NextNext;
    }

    public int PeekThree()
    {
        return NextNextNext;
    }

    public int PeekFour()
    {
        return NextNextNextNext;
    }
    
    public int Read()
    {
        if (Next == -1) throw new InvalidOperationException("You must call Init() before reading.");
        
        var result = Next;
        if (result is 10)
        {
            Line++;
            Column = 1;
        }
        else
        {
            Column++;
        }
        
        Next = NextNext;
        NextNext = NextNextNext;
        NextNextNext = NextNextNextNext;
        try
        {
            if (!Reader.EndOfStream) NextNextNextNext = PreProcessInput(Reader.Read());
            else NextNextNextNext = -1;
        }
        catch (ObjectDisposedException)
        {
            NextNextNextNext = -1;
        }
        
        return result;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#input-preprocessing
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private int PreProcessInput(int result)
    {
        // if (result is 13 && base.Peek() is 10)
        // {
        //     return Read();
        // }
        if (result is 10 or 12 or 13) // If result as char is \n, or \f or \r
        {
            // Normalise line endings to \n
            result = 10;
        }

        if (result is 0 || char.IsSurrogate((char)result)) // If result as a char is \0 (NULL)
        {
            // Normalise to U+FFFD REPLACEMENT CHARACTER
            result = 65533;
        }

        return result;
    }
    
    public void Dispose()
    {
        Reader.Dispose();
        GC.SuppressFinalize(this);
    }
}