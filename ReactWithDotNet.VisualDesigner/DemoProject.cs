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