namespace ReactWithDotNet.VisualDesigner;

record VisualElementTreeOperationMoveResponse
{
    public VisualElementModel NewRoot { get; init; }

    public ApplicationSelectionState Selection { get; init; }
}

static class VisualElementTreeOperation
{
    public static Result<VisualElementTreeOperationMoveResponse> Move(VisualElementModel root, string source, string target, DragPosition position)
    {
        // root check
        {
            if (source == "0")
            {
                return new Exception("Root node cannot move.");
            }
        }

        // parent - child control
        {
            if (target.StartsWith(source, StringComparison.OrdinalIgnoreCase))
            {
                return new Exception("Parent node cannot add to child.");
            }
        }

        // same target control
        {
            if (source == target)
            {
                return new VisualElementTreeOperationMoveResponse();
            }
        }

        var isTryingToMakeRoot = target == "0" && position == DragPosition.Before;

        VisualElementModel sourceNodeParent;
        int sourceNodeIndex;
        {
            var temp = root;

            var indexArray = source.Split(',');

            var length = indexArray.Length - 1;
            for (var i = 1; i < length; i++)
            {
                temp = temp.Children[int.Parse(indexArray[i])];
            }

            sourceNodeIndex = int.Parse(indexArray[length]);

            sourceNodeParent = temp;
        }

        if (isTryingToMakeRoot)
        {
            var rootNode = sourceNodeParent.Children[sourceNodeIndex];

            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            rootNode.Children.Add(root);

            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot = rootNode,
                Selection = new()
                {
                    VisualElementTreeItemPath = "0"
                }
            };
        }

        VisualElementModel targetNodeParent;
        int targetNodeIndex;
        {
            var temp = root;

            var indexArray = target.Split(',');

            var length = indexArray.Length - 1;
            for (var i = 1; i < length; i++)
            {
                temp = temp.Children[int.Parse(indexArray[i])];
            }

            targetNodeIndex = int.Parse(indexArray[length]);

            targetNodeParent = temp;
        }

        if (position == DragPosition.Inside)
        {
            var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

            var targetNode = targetNodeParent.Children[targetNodeIndex];

            if (targetNode.Children.Count > 0)
            {
                return new Exception("Select valid location");
            }

            // remove from source
            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            if (targetNode.HasNoChild())
            {
                targetNode.Children.Add(sourceNode);

                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot   = root,
                    Selection = new()
                };
            }
        }

        // is same parent
        if (sourceNodeParent == targetNodeParent)
        {
            if (position == DragPosition.After && sourceNodeIndex - targetNodeIndex == 1)
            {
                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot = root
                };
            }

            if (position == DragPosition.Before && targetNodeIndex - sourceNodeIndex == 1)
            {
                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot = root
                };
            }
        }

        {
            var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

            // remove from source
            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            if (sourceNodeParent == targetNodeParent)
            {
                // is adding end
                if (position == DragPosition.After && targetNodeIndex == targetNodeParent.Children.Count)
                {
                    targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

                    return new VisualElementTreeOperationMoveResponse
                    {
                        NewRoot   = root,
                        Selection = new()
                    };
                }

                if (position == DragPosition.After && targetNodeIndex == 0)
                {
                    targetNodeIndex++;
                }

                if (position == DragPosition.Before && targetNodeIndex == targetNodeParent.Children.Count)
                {
                    targetNodeIndex--;
                }
            }

            // insert into target
            targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot   = root,
                Selection = new()
            };
        }
    }

    public static Result<VisualElementTreeOperationMoveResponse> Move2(VisualElementModel root, string source, string target, DragPosition position)
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

        var sourcePath = source.Split(',');
        var targetPath = target.Split(',');

        var sourceNode = getNode(root, sourcePath);

        // root yapma durumu
        if (target == "0" && position == DragPosition.Before)
        {
            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot = sourceNode with
                {
                    Children = sourceNode.Children.Append(root).ToList()
                },
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

        return new VisualElementTreeOperationMoveResponse
        {
            NewRoot   = insert(rootWithoutSource, targetPath, sourceNode, position),
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

        static VisualElementModel insert(VisualElementModel node, string[] path, VisualElementModel toInsert, DragPosition pos)
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
                            throw new InvalidOperationException("Select valid location");
                        }

                        children[index] = targetNode with
                        {
                            Children = [toInsert]
                        };
                        break;
                }

                return node with { Children = children };
            }

            var updated = insert(node.Children[index], path[1..], toInsert, pos);
            var updatedChildren = node.Children.Select((c, i) => i == index ? updated : c).ToList();
            return node with { Children = updatedChildren };
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