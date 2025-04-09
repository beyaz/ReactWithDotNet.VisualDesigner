using React;

namespace React
{
    class ReactNode
    {
        
    }

    class MouseEvent
    {
        
    }

    delegate void MouseEventHandler(MouseEvent e);
    
    class div
    {
        public bool disabled;
        
        public MouseEventHandler onClick;
    }
}

namespace DemoProject
{
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
}