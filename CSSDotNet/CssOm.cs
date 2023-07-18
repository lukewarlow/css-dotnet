namespace CSSDotNet;

// TODO separate out the AST classes into their own files

public class CssRule
{
    
}

public class CssGroupRule : CssRule
{
    
}

public class CssStyleRule : CssGroupRule
{
    
}

public class CssImportRule : CssRule
{
    
}

// https://drafts.csswg.org/css-conditional-3/#the-cssconditionrule-interface
public class CssConditionRule : CssRule
{
    public string ConditionText { get; set; }
}

// https://drafts.csswg.org/css-conditional-3/#the-cssmediarule-interface
public class CssMediaRule : CssRule
{
    
}

public class MediaList
{
    public string? MediaText { get; set; }
    public ulong Length { get; set; }
    
    public string? GetItem(ulong index) => null;
    public void AppendMedium(string medium) { }
    public void DeleteMedium(string medium) { }
}

// https://drafts.csswg.org/css-conditional-3/#the-csssupportsrule-interface
public class CssSupportsRule : CssRule
{
    
}

