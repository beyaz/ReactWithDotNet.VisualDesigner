﻿using System.Threading;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Primitive;

sealed class LogoutButton : Component<LogoutButton.State>
{
    protected override Element render()
    {
        var theme = new
        {
            Color      = Theme.BorderColor,
            ColorHover = "#db081b"
        };

        var svgColor = new[]
        {
            Fill(state.IsMouseEntered ? theme.ColorHover : theme.Color)
        };

        return new Tooltip
        {
            Tooltip.Title("Close"),

            new div(OnMouseEnter(OnMouseEntered), OnMouseLeave(OnMouseLeaved))
            {
                new svg(Size(24), ViewBox(0, 0, 64, 64), OnClick(OnClicked))
                {
                    new path(svgColor) { d = "M40.36 9.79997C40.1077 9.70703 39.8391 9.66595 39.5705 9.67918C39.3019 9.69241 39.0387 9.75968 38.7967 9.87696C38.5547 9.99425 38.3388 10.1591 38.162 10.3618C37.9851 10.5644 37.851 10.8006 37.7676 11.0563C37.6841 11.312 37.6531 11.5819 37.6764 11.8498C37.6997 12.1177 37.7767 12.3782 37.903 12.6157C38.0293 12.8532 38.2021 13.0627 38.4112 13.2318C38.6203 13.401 38.8614 13.5262 39.12 13.6C44.2626 15.2929 48.6336 18.7628 51.4491 23.3872C54.2645 28.0116 55.3401 33.4879 54.4828 38.8336C53.6255 44.1794 50.8915 49.0447 46.7712 52.5569C42.651 56.069 37.4141 57.9982 32 57.9982C26.586 57.9982 21.3491 56.069 17.2289 52.5569C13.1086 49.0447 10.3746 44.1794 9.51731 38.8336C8.66003 33.4879 9.73557 28.0116 12.551 23.3872C15.3664 18.7628 19.7375 15.2929 24.88 13.6C25.1387 13.5262 25.3797 13.401 25.5888 13.2318C25.798 13.0627 25.9708 12.8532 26.0971 12.6157C26.2233 12.3782 26.3004 12.1177 26.3237 11.8498C26.347 11.5819 26.3159 11.312 26.2325 11.0563C26.1491 10.8006 26.0149 10.5644 25.8381 10.3618C25.6613 10.1591 25.4454 9.99425 25.2034 9.87696C24.9614 9.75968 24.6982 9.69241 24.4296 9.67918C24.1609 9.66595 23.8924 9.70703 23.64 9.79997C17.5923 11.788 12.4511 15.8663 9.13882 21.3028C5.82657 26.7394 4.56017 33.1784 5.56687 39.4644C6.57357 45.7504 9.78747 51.4719 14.6318 55.6021C19.4762 59.7324 25.6339 62.0011 32 62.0011C38.3661 62.0011 44.5239 59.7324 49.3682 55.6021C54.2126 51.4719 57.4265 45.7504 58.4332 39.4644C59.4399 33.1784 58.1735 26.7394 54.8613 21.3028C51.549 15.8663 46.4077 11.788 40.36 9.79997V9.79997Z" },
                    new path(svgColor) { d = "M32 25.06C32.5304 25.06 33.0391 24.8493 33.4142 24.4742C33.7893 24.0991 34 23.5904 34 23.06V4C34 3.46957 33.7893 2.96086 33.4142 2.58579C33.0391 2.21071 32.5304 2 32 2C31.4696 2 30.9609 2.21071 30.5858 2.58579C30.2107 2.96086 30 3.46957 30 4V23.06C30 23.5904 30.2107 24.0991 30.5858 24.4742C30.9609 24.8493 31.4696 25.06 32 25.06Z" }
                }
            }
        };
    }

    static void ExitAfterThreeSeconds()
    {
        new Thread(() =>
        {
            Thread.Sleep(3000);
            Environment.Exit(1);
        }).Start();
    }

    Task OnClicked(MouseEvent _)
    {
        Client.RunJavascript("window.close();");

        ExitAfterThreeSeconds();

        return Task.CompletedTask;
    }

    Task OnMouseEntered(MouseEvent e)
    {
        state = new State
        {
            IsMouseEntered = true
        };

        return Task.CompletedTask;
    }

    Task OnMouseLeaved(MouseEvent e)
    {
        state = new State
        {
            IsMouseEntered = false
        };

        return Task.CompletedTask;
    }

    internal sealed record State
    {
        public bool IsMouseEntered { get; init; }
    }
}