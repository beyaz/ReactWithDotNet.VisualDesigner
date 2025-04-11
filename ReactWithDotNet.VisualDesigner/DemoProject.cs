using React;

namespace DemoProject;

class CircleImageButtonProps
{
    public bool? isDisabled;

    public Action onClick;
        
    public ReactNode children;
}

class NumericUpDownProps
{
    public int? value;
        
    public Action onDownClick;
        
    public Action onUpClick;
}

class TravelersEditorProps
{
    public int adultCount;
    
    public int childrenCount;
    
    public int infantCount;

    public bool isPetFriendly;
        
    public Action onApply;
        
    public Action onReset;
}

class TravelersEditorState
{
    public int adultCount;
    
    public int childrenCount;
    
    public int infantCount;

    public bool isPetFriendly;
}


class DestinationEditorProps
{
    public int stayDayCount;
}

class DestinationEditorState
{
    public int stayDayCount;
    
    public int isInSearchMode;
}