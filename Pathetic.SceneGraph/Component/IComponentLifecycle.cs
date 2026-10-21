namespace Pathetic.SceneGraph.Component;

internal interface IComponentLifecycle
{
    void Setup();
    void LateSetup();
    void Update(float deltaTime);
    void LateUpdate(float deltaTime);
    void GUI();
    void Cleanup();
}