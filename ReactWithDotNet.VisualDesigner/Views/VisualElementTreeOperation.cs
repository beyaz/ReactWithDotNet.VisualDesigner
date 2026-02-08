namespace ReactWithDotNet.VisualDesigner.Views;

record VisualElementTreeOperationMoveResponse
{
    public VisualElementModel NewRoot { get; init; }

    public ApplicationSelectionState Selection { get; init; }
}

static class VisualElementTreeOperation
{
    public static Result<VisualElementTreeOperationMoveResponse> Move(VisualElementModel root, string source, string target, DragPosition position)
    {
        if (source == "0")
        {
            return new Exception("Root node cannot move.");
        }

        if (target.StartsWith(source + ",", StringComparison.OrdinalIgnoreCase))
        {
            return new Exception("Parent node cannot add to child.");
        }

        if (source == target)
        {
            return new VisualElementTreeOperationMoveResponse { NewRoot = root };
        }

        var sourcePath = source.Split('_');
        var targetPath = target.Split('_');

        var sourceNode = getNode(root, sourcePath);

        // root yapma durumu
        if (target == "0" && position == DragPosition.Before)
        {
            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot = sourceNode,
                Selection = new() { VisualElementTreeItemPath = "0" }
            };
        }

        // hedef indeks ve aynı parent kontrolü (source silinmeden önce)
        var sameParent = isSameParent(sourcePath, targetPath);
        var originalTargetIndex = int.Parse(targetPath[^1]);

        var rootWithoutSource = remove(root, sourcePath);

        // Eğer aynı parent içindeyse ve source index < target index ise, target index 1 azalmalı
        if (sameParent && int.Parse(sourcePath[^1]) < originalTargetIndex)
        {
            targetPath[^1] = (originalTargetIndex - 1).ToString();
        }

        return
            from newRoot in insert(rootWithoutSource, targetPath, sourceNode, position)
            select new VisualElementTreeOperationMoveResponse
            {
                NewRoot   = newRoot,
                Selection = new()
            };

        static VisualElementModel getNode(VisualElementModel node, string[] path)
        {
            for (var i = 1; i < path.Length; i++)
            {
                node = node.Children[int.Parse(path[i])];
            }

            return node;
        }

        static VisualElementModel remove(VisualElementModel node, string[] path)
        {
            var index = int.Parse(path[1]);

            if (path.Length == 2)
            {
                var newChildren = node.Children.Where((_, i) => i != index).ToList();
                return node with { Children = newChildren };
            }

            var updatedChild = remove(node.Children[index], path[1..]);
            var children = node.Children.Select((c, i) => i == index ? updatedChild : c).ToList();
            return node with { Children = children };
        }

        static Result<VisualElementModel> insert(VisualElementModel node, string[] path, VisualElementModel toInsert, DragPosition pos)
        {
            var index = int.Parse(path[1]);

            if (path.Length == 2)
            {
                var children = node.Children.ToList();

                switch (pos)
                {
                    case DragPosition.Before:
                        children.Insert(index, toInsert);
                        break;
                    case DragPosition.After:
                        children.Insert(index + 1, toInsert);
                        break;
                    case DragPosition.Inside:
                        var targetNode = children[index];
                        if (targetNode.Children.Count > 0)
                        {
                            return new InvalidOperationException("Select valid location");
                        }

                        children[index] = targetNode with
                        {
                            Children = [toInsert]
                        };
                        break;
                }

                return node with { Children = children };
            }

            return
                from updated in insert(node.Children[index], path[1..], toInsert, pos)

                let updatedChildren = node.Children.Select((c, i) => i == index ? updated : c).ToList()

                select node with { Children = updatedChildren };
        }

        static bool isSameParent(string[] a, string[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length - 1; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}