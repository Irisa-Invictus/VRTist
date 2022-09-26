/* MIT License
 *
 * © Dada ! Animation
 * &
 * Université de Rennes 1 / Invictus Project
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Dada.URig
{
    public class Processor
    {
        public float scaleFactor = 1f;

        public string[] locControllerPath = new string[] { "rig_grp", "anim_grp", "loc_ctrl" };
        public string[] movControllerPath = new string[] { "rig_grp", "anim_grp", "loc_ctrl", "mov_ctrl" };
        public Transform root;


        private bool TryGetTransformByPath(string[] path, Transform itTransform, out Transform transform)
        {
            foreach (var segment in path)
            {
                Transform child = itTransform.Find(segment);
                if (child == null)
                {
                    transform = null;
                    return false;
                }

                itTransform = child;
            }

            transform = itTransform;
            return true;
        }

        private Transform GetTransformByPath(string[] path)
        {
            if (!TryGetTransformByPath(path, root.transform, out Transform transform))
            {
                foreach (Transform childs in root.transform)
                {
                    if (TryGetTransformByPath(path, childs, out transform)) return transform;
                }
            }
            return transform;
        }

        private static Vector3 MapJSONVec3ToVector3(float[] vector)
        {
            return new Vector3(vector[0], vector[1], vector[2]).normalized;
        }

        private void InitializeLimitRanges(VRtist.RigConstraintController controller)
        {
            var localPosition = controller.transform.localPosition * scaleFactor;
            var localEulerAngles = controller.transform.localEulerAngles;
            var localScale = controller.transform.localScale;

            controller.xTranslationRange = new Range(localPosition.x);
            controller.yTranslationRange = new Range(localPosition.y);
            controller.zTranslationRange = new Range(localPosition.z);
            controller.xRotationRange = new Range(localEulerAngles.x);
            controller.yRotationRange = new Range(localEulerAngles.y);
            controller.zRotationRange = new Range(localEulerAngles.z);
            controller.xScaleRange = new Range(localScale.x);
            controller.yScaleRange = new Range(localScale.y);
            controller.zScaleRange = new Range(localScale.z);
        }

        private static void ApplyLimits(VRtist.RigConstraintController sceneObjectPart, JSONDescriptors.ObjectAttribute attribute)
        {
            Range limits = attribute.range;
            switch (attribute.name)
            {
                case "TX":
                    sceneObjectPart.xTranslationRange = limits;
                    break;

                case "TY":
                    sceneObjectPart.yTranslationRange = limits;
                    break;

                case "TZ":
                    sceneObjectPart.zTranslationRange = limits;
                    break;

                case "RX":
                    sceneObjectPart.xRotationRange = limits;
                    break;

                case "RY":
                    sceneObjectPart.yRotationRange = limits;
                    break;

                case "RZ":
                    sceneObjectPart.zRotationRange = limits;
                    break;

                case "SX":
                    sceneObjectPart.xScaleRange = limits;
                    break;

                case "SY":
                    sceneObjectPart.yScaleRange = limits;
                    break;

                case "SZ":
                    sceneObjectPart.zScaleRange = limits;
                    break;

                default:
                    throw new InvalidAttributeNameExeption(attribute.name);
            }
        }


        public void CreateControllers(Transform root, string path)
        {
            this.root = root;
            var skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            StreamReader reader = new StreamReader(path);
            string text = reader.ReadToEnd();
            JSONDescriptors.Rig rigDescriptor = JsonConvert.DeserializeObject<JSONDescriptors.Rig>(text);


            if (rigDescriptor.controllers != null)
            {
                foreach (var objectDescriptor in rigDescriptor.controllers)
                {
                    var childTransform = GetTransformByPath(objectDescriptor.path);

                    VRtist.RigConstraintController controller = childTransform.gameObject.AddComponent<VRtist.RigConstraintController>();
                    if (childTransform.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = false;
                    if (childTransform.TryGetComponent(out Collider collider)) collider.enabled = false;
                    controller.gameObject.tag = "Controller";

                    InitializeLimitRanges(controller);
                    foreach (JSONDescriptors.ObjectAttribute attributeDescriptor in objectDescriptor.attributes)
                    {
                        ApplyLimits(controller, attributeDescriptor);
                    }

                    var constraints = new List<Descriptors.Constraint>();

                    foreach (var constraintDescriptor in objectDescriptor.constraints)
                    {
                        var targetTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath);

                        if (constraintDescriptor.aim != null)
                        {
                            var constraintDescriptorVariant = constraintDescriptor.aim;

                            if (constraintDescriptorVariant.up != null)
                            {
                                if (constraintDescriptorVariant.up.aim != null)
                                {
                                    var upTargetDescriptorVariant = constraintDescriptorVariant.up.aim;

                                    var localToDrivenForward = MapJSONVec3ToVector3(constraintDescriptorVariant.vector);
                                    var localToDrivenPreUpward = MapJSONVec3ToVector3(constraintDescriptorVariant.up.vector);
                                    var localToDrivenSide = Vector3.Cross(localToDrivenForward, localToDrivenPreUpward).normalized;
                                    var localToDrivenUpward = Vector3.Cross(localToDrivenSide, localToDrivenForward).normalized;
                                    var localToDrivenMatrix = new Matrix4x4(localToDrivenForward, localToDrivenUpward, localToDrivenSide, Vector4.zero);
                                    localToDrivenMatrix[15] = 1f;

                                    constraints.Add(new Descriptors.Constraint
                                    {
                                        drivenObjectTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath),
                                        type = Descriptors.Constraint.Type.AimUpAim,
                                        aimUpAim = new Descriptors.AimUpConstraint
                                        {
                                            upTransform = GetTransformByPath(upTargetDescriptorVariant.targetPath),
                                            localToDrivenMatrix = localToDrivenMatrix,
                                        },
                                    });
                                }

                                else if (constraintDescriptorVariant.up.rotate != null)
                                {
                                    var upTargetDescriptorVariant = constraintDescriptorVariant.up.rotate;

                                    // TODO
                                }

                                else
                                {
                                    throw new MissingOneOfVariantException<JSONDescriptors.AimConstraintUp>();
                                }
                            }
                        }

                        else if (constraintDescriptor.copyAttributes != null)
                        {
                            JSONDescriptors.CopyAttributesConstraint constraintDescriptorVariant = constraintDescriptor.copyAttributes;

                            foreach (var attribute in constraintDescriptorVariant.drivenAttributes)
                            {

                                constraints.Add(new Descriptors.Constraint
                                {
                                    drivenObjectTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath),
                                    type = Descriptors.Constraint.Type.CopyWorldAttributeToTransformAttribute,
                                    copyWorldAttributeToTransformAttribute = new Descriptors.CopyWorldAttributeToTransformAttributeConstraint
                                    {
                                        attributeName = attribute.name,
                                        target = new Descriptors.ObjectAttribute
                                        {
                                            name = attribute.name,
                                            range = attribute.range
                                        }
                                    },
                                });
                            }
                        }

                        else if (constraintDescriptor.cursor != null)
                        {
                            var constraintDescriptorVariant = constraintDescriptor.cursor;

                            if (constraintDescriptorVariant.target.blendShape != null)
                            {
                                var targetDescriptorVariant = constraintDescriptorVariant.target.blendShape;

                                string blendShapeName = constraintDescriptor.drivenObjectPath + "." + targetDescriptorVariant.name;
                                bool found = false;
                                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                                {
                                    for (int blendShapeIndex = 0; blendShapeIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount; ++blendShapeIndex)
                                    {
                                        if (skinnedMeshRenderer.sharedMesh.GetBlendShapeName(blendShapeIndex) == blendShapeName)
                                        {
                                            constraints.Add(new Descriptors.Constraint
                                            {
                                                drivenObjectTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath),
                                                type = Descriptors.Constraint.Type.CopyLocalAttributeToBlendShapeWeight,
                                                copyLocalAttributeToBlendShapeWeight = new Descriptors.CopyLocalAttributeToBlendShapeWeightConstraint
                                                {
                                                    attributeName = constraintDescriptorVariant.attributeName,
                                                    blendShapeIndex = blendShapeIndex,
                                                    skinnedMeshRenderer = skinnedMeshRenderer,
                                                    range = targetDescriptorVariant.range,
                                                },
                                            });
                                            found = true;
                                            break;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    Debug.LogWarningFormat("No blend shapes {0} were found.", blendShapeName);
                                }
                            }
                            else if (constraintDescriptorVariant.target.@object != null)
                            {
                                var targetDescriptorVariant = constraintDescriptorVariant.target.@object;

                                constraints.Add(new Descriptors.Constraint
                                {
                                    drivenObjectTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath),
                                    type = Descriptors.Constraint.Type.CopyLocalAttributeToTransformAttribute,
                                    copyLocalAttributeToTransformAttribute = new Descriptors.CopyLocalAttributeToTransformAttributeConstraint
                                    {
                                        attributeName = constraintDescriptorVariant.attributeName,
                                        target = new Descriptors.ObjectAttribute
                                        {
                                            name = targetDescriptorVariant.name,
                                            range = targetDescriptorVariant.range,
                                        }
                                    },
                                });
                            }
                            else
                            {
                                throw new MissingOneOfVariantException<JSONDescriptors.CursorTarget>();
                            }
                        }

                        else if (constraintDescriptor.parent != null)
                        {
                            JSONDescriptors.ParentConstraint constraintDescriptorVariant = constraintDescriptor.parent;

                            Transform drivenObjectTransform = GetTransformByPath(constraintDescriptor.drivenObjectPath);
                            //Matrix4x4 localToTargetMatrix = drivenObjectTransform.localToWorldMatrix * childTransform.parent.worldToLocalMatrix;
                            Matrix4x4 localToTargetMatrix = drivenObjectTransform.localToWorldMatrix * childTransform.worldToLocalMatrix;

                            Range getRange(string attributeName)
                            {
                                // Return blocking range if nonexistent.
                                return constraintDescriptorVariant.drivenAttributes.FirstOrDefault(attribute => attribute.name == "RX")?.range ?? new Range(0f);
                            }

                            constraints.Add(new Descriptors.Constraint
                            {
                                drivenObjectTransform = drivenObjectTransform,
                                type = Descriptors.Constraint.Type.Parent,
                                parent = new Descriptors.ParentConstraint
                                {
                                    localToTargetMatrix = localToTargetMatrix,
                                    xTranslationRange = getRange("TX"),
                                    yTranslationRange = getRange("TY"),
                                    zTranslationRange = getRange("TZ"),
                                    xRotationRange = getRange("RX"),
                                    yRotationRange = getRange("RY"),
                                    zRotationRange = getRange("RZ"),
                                    xScaleRange = getRange("SX"),
                                    yScaleRange = getRange("SY"),
                                    zScaleRange = getRange("SZ"),
                                }
                            });
                        }

                        else
                        {
                            throw new MissingOneOfVariantException<JSONDescriptors.Constraint>();
                        }

                        controller.constraints = constraints.ToArray();
                    }
                }
            }
        }

    }
}
