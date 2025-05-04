using ReactWithDotNet.ThirdPartyLibraries.FramerMotion;

namespace ReactWithDotNet.VisualDesigner.Primitive;

static class Animation
{
    public static Element AnimateVisibility(bool isVisible, Element content)
    {
        return new AnimatePresence
        {
            !isVisible
                ? null
                : new motion.div
                {
                    initial =
                    {
                        height  = 0,
                        opacity = 0,
                        scale   = 0.5
                    },
                    animate =
                    {
                        height  = "auto",
                        opacity = 1,
                        scale   = 1
                    },
                    exit =
                    {
                        height  = 0,
                        opacity = 0,
                        scale   = 0.5
                    },

                    style = { WidthFull },
                    children =
                    {
                        content
                    }
                }
        };
    }
}