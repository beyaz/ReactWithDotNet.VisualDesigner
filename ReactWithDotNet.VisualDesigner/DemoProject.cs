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


    public Action onAdultCountMinus;
    public Action onAdultCountPlus;
    
    public Action onChildrenCountMinus;
    public Action onChildrenCountPlus;
    
    public Action onInfantCountMinus;
    public Action onInfantCountPlus;
}

class TravelersEditorState
{
    public int adultCount;
    
    public int childrenCount;
    
    public int infantCount;

    public bool isPetFriendly;
        
    public Action onApply;
        
    public Action onReset;


    public Action onAdultCountMinus;
    public Action onAdultCountPlus;
    
    public Action onChildrenCountMinus;
    public Action onChildrenCountPlus;
    
    public Action onInfantCountMinus;
    public Action onInfantCountPlus;
}