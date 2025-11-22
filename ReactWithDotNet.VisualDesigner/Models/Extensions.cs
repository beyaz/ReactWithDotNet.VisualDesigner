global using static ReactWithDotNet.VisualDesigner.Models.Extensions;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Extensions
{
    extension(VisualElementModel model)
    {
        public string Text
        {
            get
            {
                var query =
                    from p in model.Properties
                    from v in TryParseProperty(p)
                    where v.Name == Design.Content
                    select v.Value;

                return query.FirstOrDefault();
            }
        }
    }

    public static bool HasNoChild(this VisualElementModel model)
    {
        return model.Children is null || model.Children.Count == 0;
    }

    public static bool HasNoText(this VisualElementModel model)
    {
        return model.Text.HasNoValue();
    }

    public static bool HasText(this VisualElementModel model)
    {
        return model.Text.HasValue();
    }

    public static VisualElementModel Modify(VisualElementModel root, VisualElementModel target, Func<VisualElementModel, VisualElementModel> modifyNode)
    {
        if (root == target)
        {
            return modifyNode(root);
        }

        return root with
        {
            Children = ListFrom(from child in root.Children select Modify(child, target, modifyNode))
        };
    }

    public static VisualElementModel ModifyElements(VisualElementModel root, Func<VisualElementModel, bool> match, Func<VisualElementModel, VisualElementModel> modify)
    {
        // Eğer kök eleman match fonksiyonuna uyuyorsa modify et
        var modifiedRoot = match(root) ? modify(root) : root;

        // Çocuk elemanları yine bu fonksiyonla recursive olarak işleyin
        var modifiedChildren = modifiedRoot.Children
            .Select(child => ModifyElements(child, match, modify))
            .ToList();

        // Yeni bir VisualElementModel oluştur ve değiştirilen çocukları ekle
        return modifiedRoot with { Children = modifiedChildren };
    }

    /// <summary>
    ///     Bir öğeyi, listede başka bir öğenin önüne veya arkasına taşır.
    ///     Drag-and-drop gibi işlemler için idealdir.
    /// </summary>
    public static IReadOnlyList<T> MoveItemRelativeTo<T>(this IReadOnlyList<T> list, int sourceIndex, int targetIndex, bool insertBefore)
    {
        if (list == null || sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 ||
            sourceIndex >= list.Count || targetIndex >= list.Count)
        {
            return list;
        }

        var item = list[sourceIndex];

        list = list.RemoveAt(sourceIndex);

        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        var insertIndex = insertBefore ? targetIndex : targetIndex + 1;

        if (insertIndex > list.Count)
        {
            insertIndex = list.Count;
        }

        if (insertIndex < 0)
        {
            insertIndex = 0;
        }

        return list.Insert(insertIndex, item);
    }

    public static Maybe<int> TryReadTagAsDesignerComponentId(VisualElementModel model)
    {
        if (int.TryParse(model.Tag, out var componentId))
        {
            return componentId;
        }

        return None;
    }
}