using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using System.Linq;
using System.Collections.Generic;

namespace MSALoader.Core
{
    public class AnimatorController
    {
        public Animator selectedAnimator;
        private float animatorTime = 0f;
        private Dictionary<string, AnimationClip> externalAnimations = new Dictionary<string, AnimationClip>();

        /// <summary>
        /// Establece el Animator seleccionado.
        /// </summary>
        public void SetSelectedAnimator(Animator animator)
        {
            selectedAnimator = animator;
            if (selectedAnimator != null)
            {
                animatorTime = selectedAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                MSLoader.Logger.LogInfo($"Animator asignado: {selectedAnimator.gameObject.name}");
            }
            else
            {
                animatorTime = 0f;
                MSLoader.Logger.LogWarning("No se encontró Animator en el objeto seleccionado.");
            }
        }

        public async Task ChangeAnimationFromBundle(AnimationClip animation)
        {
            if (animation == null)
            {
                MSLoader.Logger.LogError("La animación cargada es nula.");
                return;
            }

            MSLoader.Logger.LogWarning($"Nombre de animación: {animation.name}");
            MSLoader.Logger.LogWarning($"Duración de animación: {animation.length}");
            MSLoader.Logger.LogWarning($"FrameRate de animación: {animation.frameRate}");

            await ApplyExternalAnimation(animation);
        }

        /// <summary>
        /// Cambia a una animación específica.
        /// </summary>

        public async Task ChangeAnimation(string animationName)
        {
            if (selectedAnimator == null)
            {
                MSLoader.Logger.LogWarning("No hay un Animator seleccionado.");
                return;
            }

            // Buscar en animaciones externas
            if (externalAnimations.TryGetValue(animationName, out AnimationClip externalClip))
            {
                await ApplyExternalAnimation(externalClip);
                MSLoader.Logger.LogInfo($"Animación externa aplicada: {externalClip.name}");
                return;
            }


            var clip = selectedAnimator.runtimeAnimatorController.animationClips.FirstOrDefault(x => x.name == animationName);
            if (clip != null)
            {
                MSLoader.Logger.LogInfo($"AnimationPlayer PlayAnimation: {animationName}");

                AnimationPlayer.PlayAnimation(selectedAnimator, clip);

                //TestAnimPlayable(selectedAnimator, clip);
            }
            MSLoader.Logger.LogInfo($"Reproduciendo animación interna: {animationName}");
            return;


            // Buscar en el controlador del Animator
            if (selectedAnimator.HasState(0, Animator.StringToHash(animationName)))
            {

                //selectedAnimator.Play(animationName, -1, 0f);
                //var currentStateInfo = selectedAnimator.GetCurrentAnimatorStateInfo(0);
                //if (currentStateInfo.length > 0)
                //{
                //    MSLoader.Logger.LogInfo($"Current animation state: {animationName}, normalized time: {currentStateInfo.normalizedTime}");
                //}
                //else
                //{
                //    MSLoader.Logger.LogWarning("Current animator state info is not valid");
                //}
                //
                //// Verificar si hay un PlayableGraph activo
                //PlayableGraph currentGraph = selectedAnimator.playableGraph;
                //if (currentGraph.IsValid())
                //{
                //    MSLoader.Logger.LogInfo("Current PlayableGraph is valid");
                //    currentGraph.Stop();
                //}

                //var clip = selectedAnimator.runtimeAnimatorController.animationClips.FirstOrDefault(x => x.name == animationName);
                if (clip != null)
                {
                    MSLoader.Logger.LogInfo($"AnimationPlayer PlayAnimation: {animationName}");

                    AnimationPlayer.PlayAnimation(selectedAnimator, clip);

                    //TestAnimPlayable(selectedAnimator, clip);
                }
                MSLoader.Logger.LogInfo($"Reproduciendo animación interna: {animationName}");
            }
            else
            {
                MSLoader.Logger.LogWarning($"Animación no encontrada: {animationName}");
            }
        }


        public static void TestAnimPlayable(Animator _anitor, AnimationClip _anim)
        {
            //Playable _playable = new();
            //PlayableGraph playableGraph = new();
            UnityEngine.Playables.AnimationPlayableUtilities.PlayClip(_anitor, _anim, out PlayableGraph _);
        }


        /// <summary>
        /// Aplica una animación externa al Animator.
        /// </summary>
        private async Task ApplyExternalAnimation(AnimationClip clip)
        {
            if (selectedAnimator == null || clip == null)
            {
                MSLoader.Logger.LogWarning("Animator o AnimationClip no están configurados.");
                await Task.Yield();
                return;
            }

            try
            {
                // Configurar el WrapMode para permitir bucles
                //clip.wrapMode = WrapMode.Loop;

                // Reproducir la animación usando el sistema de Playables
                AnimationPlayer.PlayAnimation(selectedAnimator, clip);

                MSLoader.Logger.LogInfo($"Animación externa aplicada: {clip.name}");

                return;

                // Verificar si el Animator tiene un RuntimeAnimatorController
                if (selectedAnimator.runtimeAnimatorController == null)
                {
                    MSLoader.Logger.LogWarning("Asignando un controlador base predeterminado.");
                    //RuntimeAnimatorController defaultController = Resources.Load<RuntimeAnimatorController>("DefaultAnimatorController");
                    //if (defaultController != null)
                    //{
                    //    selectedAnimator.runtimeAnimatorController = defaultController;
                    //}
                    //else
                    //{
                    //    MSLoader.Logger.LogError("No se encontró un controlador base predeterminado.");
                    //    return;
                    //}
                }

                // Crear un AnimatorOverrideController con el controlador base actual
                var runtimeController = selectedAnimator.runtimeAnimatorController.Cast<RuntimeAnimatorController>();
                if (runtimeController == null)
                {
                    MSLoader.Logger.LogError("El controlador de animaciones del Animator es nulo.");
                    return;
                }

                var overrideController = new AnimatorOverrideController(Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(runtimeController));

                // Aplicar la animación externa
                overrideController[clip.name] = clip;

                // Asignar el nuevo controlador al Animator
                selectedAnimator.runtimeAnimatorController = overrideController;

                // Reproducir la animación
                selectedAnimator.Play(clip.name, -1, 0f);
                MSLoader.Logger.LogInfo($"Animación externa aplicada: {clip.name}");

            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error aplicando animación externa: {ex}");
            }

            await Task.Yield();
        }


        /// <summary>
        /// Carga animaciones externas desde un AssetBundle.
        /// </summary>
        public IEnumerator LoadExternalAnimations(string bundlePath)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            AssetBundle bundle = request.assetBundle;
            if (bundle == null)
            {
                MSLoader.Logger.LogError($"Error cargando bundle: {bundlePath}");
                yield break;
            }

            foreach (var asset in bundle.LoadAllAssets())
            {
                
                if (asset is AnimationClip clip)
                {
                    // Validar compatibilidad con el Animator seleccionado
                    if (selectedAnimator != null && selectedAnimator.runtimeAnimatorController != null)
                    {
                        if (!selectedAnimator.runtimeAnimatorController.animationClips.Any(c => c.name == clip.name))
                        {
                            MSLoader.Logger.LogWarning($"La animación {clip.name} no es compatible con el Animator.");
                            continue;
                        }
                    }

                    if (!externalAnimations.ContainsKey(clip.name))
                    {
                        externalAnimations.Add(clip.name, clip);
                        MSLoader.Logger.LogInfo($"Animación cargada: {clip.name}");
                    }
                }
            }
            bundle.Unload(false); // Mantener los assets en memoria
        }

        /// <summary>
        /// Obtiene las animaciones disponibles.
        /// </summary>
        public async Task<List<string>> GetAvailableAnimations()
        {
            var animations = new List<string>();

            // Animaciones del controlador actual
            if (selectedAnimator != null && selectedAnimator.runtimeAnimatorController != null)
            {
                foreach (var clip in selectedAnimator.runtimeAnimatorController.animationClips)
                {
                    if (!animations.Contains(clip.name))
                    {
                        animations.Add(clip.name);
                    }
                    await Task.Yield();
                }
            }

            // Animaciones externas cargadas
            animations.AddRange(externalAnimations.Keys.Except(animations));

            return animations;
        }


        /// <summary>
        /// Reinicia la animación actual.
        /// </summary>
        public void ResetAnimation()
        {
            if (selectedAnimator != null)
            {
                selectedAnimator.Play(selectedAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
                animatorTime = 0f;
                MSLoader.Logger.LogInfo("Animación reiniciada.");
            }
        }

        /// <summary>
        /// Obtiene el tiempo normalizado del Animator.
        /// </summary>
        public float GetAnimatorTime()
        {
            return animatorTime;
        }

        /// <summary>
        /// Establece el tiempo normalizado del Animator.
        /// </summary>
        public void SetAnimatorTime(float time)
        {
            if (selectedAnimator != null)
            {
                selectedAnimator.Play(selectedAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, time);
                selectedAnimator.Update(0); // Forzar actualización
                animatorTime = time;
            }
        }

        /// <summary>
        /// Alterna el estado del Animator (habilitado/deshabilitado).
        /// </summary>
        public void ToggleAnimatorState()
        {
            if (selectedAnimator != null)
            {
                selectedAnimator.enabled = !selectedAnimator.enabled;
                MSLoader.Logger.LogInfo($"Animator estado: {selectedAnimator.enabled}");
            }
        }

        /// <summary>
        /// Verifica si el Animator está habilitado.
        /// </summary>
        public bool IsAnimatorEnabled()
        {
            return selectedAnimator != null && selectedAnimator.enabled;
        }

        /// <summary>
        /// Retorna el Animator seleccionado.
        /// </summary>
        public Animator GetSelectedAnimator()
        {
            return selectedAnimator;
        }


        /// <summary>
        /// Asigna un nuevo AnimatorController al objeto seleccionado.
        /// </summary>
        public void SetAnimatorController(RuntimeAnimatorController newController)
        {
            if (selectedAnimator == null)
            {
                MSLoader.Logger.LogWarning("No hay un Animator seleccionado.");
                return;
            }

            selectedAnimator.runtimeAnimatorController = newController;
            MSLoader.Logger.LogInfo($"Nuevo AnimatorController asignado: {newController.name}");
        }

        /// <summary>
        /// Aplica un AnimationClip externo al Animator.
        /// </summary>
        public void ApplyAnimationClip(AnimationClip clip)
        {
            if (selectedAnimator == null || clip == null)
            {
                MSLoader.Logger.LogWarning("Animator o AnimationClip no están configurados.");
                return;
            }

            try
            {
                // Crear un nuevo AnimatorOverrideController
                var runtimeController = selectedAnimator.runtimeAnimatorController.Cast<RuntimeAnimatorController>();
                if (runtimeController == null)
                {
                    MSLoader.Logger.LogError("El controlador de animaciones del Animator es nulo.");
                    return;
                }

                var overrideController = new AnimatorOverrideController(Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(runtimeController));
                overrideController[clip.name] = clip;

                // Asignar el nuevo controlador al Animator
                selectedAnimator.runtimeAnimatorController = overrideController;

                // Reproducir la animación
                selectedAnimator.Play(clip.name, -1, 0f);
                MSLoader.Logger.LogInfo($"AnimationClip aplicado: {clip.name}");
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error aplicando AnimationClip: {ex}");
            }
        }

        public Dictionary<string, AnimationClip> animationClips = new Dictionary<string, AnimationClip>();

        public void LoadAnimationsFromResources()
        {
            // Cargar todos los AnimationClips desde Resources
            var clips = Resources.FindObjectsOfTypeAll<AnimationClip>()
                                 .Where(clip => clip != null)
                                 .ToList();

            foreach (var clip in clips)
            {
                if (!animationClips.ContainsKey(clip.name))
                {
                    animationClips.Add(clip.name, clip);
                }
            }

            MSLoader.Logger.LogInfo($"Se cargaron {animationClips.Count} animaciones desde Resources.");
        }


    }
}
