using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class Controller : MonoBehaviour
{

    public NavMeshAgent agent;
    public ThirdPersonCharacter character;
    public SkinnedMeshRenderer rendererCharacter;
    public MeshRenderer rendererCar;

    public Building house { get; set; }
    public Building work { get; set; }
    public Building stadium { get; set; }
    public Building shop { get; set; }
    public Building target { get; set; }
    public Building current { get; set; }

    int lastArea  = 1;

    private float waitingTime = 0f;

    private bool inHouse = false, inWork = false, inOther = false, isLastModelCar = true, lastTripWasEntertainment = false;


    void Start()
    {
        rendererCharacter = this.GetComponentInChildren<SkinnedMeshRenderer>();
        rendererCar = this.GetComponentInChildren<MeshRenderer>();
        rendererCharacter.enabled = false;
        agent.updateRotation = false;
        goToWork();
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent.isStopped)
        {
            if (!HasNavMeshAgentReachedDestination())
            {
                NavMeshHit navMeshHit;
                agent.SamplePathPosition(NavMesh.AllAreas, 0f, out navMeshHit);
                if (lastArea != navMeshHit.mask)
                    areaChange(navMeshHit.mask);
                character.Move(agent.desiredVelocity, false, false);
            }
            else // agent has arrived, wait and choose next target location 
            {
                character.Move(Vector3.zero, false, false);
                agent.isStopped = true;
                if (rendererCar.enabled) isLastModelCar = true;
                else isLastModelCar = false;
                rendererCharacter.enabled = false;
                rendererCar.enabled = false;
                target.arrived();
                current = target;
                this.transform.position = new Vector3(500f, 500f, 500f);
            }
        }
        else // target is waiting at its current location
        {
            waitingTime -= Time.deltaTime;
            if (waitingTime <= 0)
            {
                chooseNextTarget();
            }
        }
    }

    // choose next location based on the current one
    private void chooseNextTarget()
    {
        if (inWork) // if at work, always return home
        {
            work.leave();
            goToHouse();
        }
        else if (inHouse) // if at home, either rest and go to work or go to an entertainment place (cinema, shopping, ...) unless the last location was entertainment
        {
            house.leave();
            float rand = Random.Range(0f, 1f);
            float randTarget = Random.Range(0f, 1f);
            if (rand <= 0.5f && !lastTripWasEntertainment) // go to an entertainment place
            {
                lastTripWasEntertainment = true;
                if (randTarget <= 0.1f) goToOther(this.stadium);
                else goToOther(this.shop);
            }
            else
            {
                lastTripWasEntertainment = false;
                goToWork();
            }
        }
        else // if at an entertainment place go back home
        {
            shop.leave();
            goToHouse();
        }
    }

    private void goToTarget()
    {
        this.transform.position = current.roadAnchor;
        agent.SetDestination(target.roadAnchor);
        if (isLastModelCar) rendererCar.enabled = true;
        agent.isStopped = false;
    }

    public void goToWork()
    {
        inHouse = false;
        inWork = true;
        inOther = false;
        target = this.work;
        waitingTime = Random.Range(8f, 12f);
        goToTarget();
    }
    public void goToHouse()
    {
        inHouse = true;
        inWork = false;
        inOther = false;
        target = this.house;
        waitingTime = Random.Range(8f, 12f);
        goToTarget();
    }

    public void goToOther(Building other)
    {
        inHouse = false;
        inWork = false;
        inOther = true;
        target = other;
        waitingTime = Random.Range(8f, 12f);
        goToTarget();
    }

    public void areaChange(int newArea)
    {
        lastArea = newArea;
        switch(newArea)
        {
            case 1: // normal road
                agent.speed = 5.5f;
                rendererCar.enabled = true;
                rendererCharacter.enabled = false;
                break;
            case 8: // highway
                agent.speed = 7f;
                rendererCar.enabled = true;
                rendererCharacter.enabled = false;
                break;
            case 16: // pedestrian
                agent.speed = 3f;
                rendererCar.enabled = false;
                rendererCharacter.enabled = true;
                break;
        }
    }

    public bool HasNavMeshAgentReachedDestination()
    {
        // need to check first if the person is spawned and on the navmesh
        if (!agent.isOnNavMesh)
        {
            Debug.Log("Not on navmesh");
            return false;
        }
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
