
public class RvoAgent
{
    public Vector2 position;
    public Vector2 velocity;
    public float radius;
    public float maxSpeed;
    public Vector2 prefVelocity;
    public Vector2 newVelocity;

    private List<Line> orcaLines = new List<Line>();

    public RvoAgent(Vector2 position, Vector2 velocity, float radius, float maxSpeed)
    {
        this.position = position;
        this.velocity = velocity;
        this.radius = radius;
        this.maxSpeed = maxSpeed;
        this.prefVelocity = velocity;
        this.newVelocity = velocity;
    }

    public void Execute(float timeHorizonObst, float timeHorizon, float timeStep, List<RvoAgent> agents)
    {
        orcaLines.Clear();
        float invTimeHorizonObst = 1.0f / timeHorizonObst;

        List<Obstacle> obstacles = Orca.QueryObstacles(position, radius * 1.5f);
        
        // Console.WriteLine($"obstacles: {obstacles.Count}");

        for (int i = 0; i < obstacles.Count; ++i)
        {
            Obstacle obstacle1 = obstacles[i];
            Obstacle obstacle2 = obstacle1.next;
            Vector2 relativePosition1 = obstacle1.point - position;
            Vector2 relativePosition2 = obstacle2.point - position;
            bool alreadyCovered = false;

            for (int j = 0; j < orcaLines.Count; ++j)
            {
                if (RvoMath.Det(invTimeHorizonObst * relativePosition1 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -RvoMath.RVO_EPSILON
                    && RvoMath.Det(invTimeHorizonObst * relativePosition2 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -RvoMath.RVO_EPSILON)
                {
                    alreadyCovered = true;
                    break;
                }
            }

            if (alreadyCovered) continue;
            float distSq1 = RvoMath.AbsSq(relativePosition1);
            float distSq2 = RvoMath.AbsSq(relativePosition2);
            float radiusSq = RvoMath.Sqr(radius);
            Vector2 obstacleVector = obstacle2.point - obstacle1.point;
            float s = RvoMath.Dot(-relativePosition1, obstacleVector) / RvoMath.AbsSq(obstacleVector);
            float distSqLine = RvoMath.AbsSq(-relativePosition1 - s * obstacleVector);

            Line line = new Line();
            
            // Console.WriteLine($"s: o1:{obstacle1.point.x:F3},{obstacle1.point.y:F3}->o2:{obstacle2.point.x:F3},{obstacle2.point.y:F3} {s}");

            if (s < 0.0f && distSq1 <= radiusSq)
            {
                if (obstacle1.convex)
                {
                    Vector2 normal = RvoMath.normalize(relativePosition1);
                    line.point = invTimeHorizonObst * relativePosition1 + invTimeHorizonObst * radius * normal;
                    line.direction = new Vector2(-normal.y, normal.x);
                    orcaLines.Add(line);
                }
                // Console.WriteLine($"line.point:{line.point.x:F3},{line.point.y:F3} line.direction:{line.direction.x},{line.direction.y}");
                continue;
            }
            else if (s > 1.0f && distSq2 <= radiusSq)
            {
                if (obstacle2.convex && RvoMath.Det(relativePosition2, obstacle2.direction) >= 0.0f)
                {
                    Vector2 normal = RvoMath.normalize(relativePosition2);
                    line.point = invTimeHorizonObst * relativePosition2 + invTimeHorizonObst * radius * normal;
                    line.direction = new Vector2(-normal.y, normal.x);
                    orcaLines.Add(line);
                }
                // Console.WriteLine($"line.point:{line.point.x:F3},{line.point.y:F3} line.direction:{line.direction.x},{line.direction.y}");
                continue;
            }
            else if (s >= 0.0f && s <= 1.0f && distSqLine <= radiusSq)
            {
                Vector2 closest = obstacle1.point + s * (obstacle2.point - obstacle1.point);
                Vector2 normal = new Vector2(obstacle1.direction.y, -obstacle1.direction.x);
                line.point = (closest - position) * invTimeHorizonObst + invTimeHorizonObst * radius * normal;
                // line.point = new Vector2(-0.1f, 0);
                line.direction = obstacle1.direction;
                orcaLines.Add(line);
            Console.WriteLine($"line.point:{line.point.x:F3},{line.point.y:F3} line.direction:{line.direction.x},{line.direction.y}");
                continue;
            }
            // Console.WriteLine($"line.point:{line.point.x:F3},{line.point.y:F3} line.direction:{line.direction.x},{line.direction.y}");
            Vector2 leftLegDirection, rightLegDirection;

            if (s < 0.0f && distSqLine <= radiusSq)
            {
                if (!obstacle1.convex) continue;
                obstacle2 = obstacle1;
                float leg1 = RvoMath.Sqrt(distSq1 - radiusSq);
                leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius, -relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
            }
            else if (s > 1.0f && distSqLine <= radiusSq)
            {
                if (!obstacle2.convex) continue;
                obstacle1 = obstacle2;
                float leg2 = RvoMath.Sqrt(distSq2 - radiusSq);
                leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius, relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
            }
            else
            {
                if (obstacle1.convex)
                {
                    float leg1 = RvoMath.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                }
                else
                {
                    leftLegDirection = -obstacle1.direction;
                }

                if (obstacle2.convex)
                {
                    float leg2 = RvoMath.Sqrt(distSq2 - radiusSq);
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    rightLegDirection = obstacle1.direction;
                }
            }

            Obstacle leftNeighbor = obstacle1.previous;
            bool isLeftLegForeign = false;
            bool isRightLegForeign = false;

            if (obstacle1.convex && RvoMath.Det(leftLegDirection, -leftNeighbor.direction) >= 0.0f)
            {
                leftLegDirection = -leftNeighbor.direction;
                isLeftLegForeign = true;
            }

            if (obstacle2.convex && RvoMath.Det(rightLegDirection, obstacle2.direction) <= 0.0f)
            {
                rightLegDirection = obstacle2.direction;
                isRightLegForeign = true;
            }

            Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point - position);
            Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point - position);
            Vector2 cutOffVector = rightCutOff - leftCutOff;
            float t = obstacle1 == obstacle2 ? 0.5f : RvoMath.Dot((velocity - leftCutOff), cutOffVector) / RvoMath.AbsSq(cutOffVector);
            float tLeft = RvoMath.Dot((velocity - leftCutOff), leftLegDirection);
            float tRight = RvoMath.Dot((velocity - rightCutOff), rightLegDirection);

            if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
            {
                Vector2 unitW = RvoMath.normalize(velocity - leftCutOff);
                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = leftCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);
                continue;
            }
            else if (t > 1.0f && tRight < 0.0f)
            {
                Vector2 unitW = RvoMath.normalize(velocity - rightCutOff);
                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = rightCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);
                continue;
            }

            float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (leftCutOff + t * cutOffVector));
            float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (leftCutOff + tLeft * leftLegDirection));
            float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (rightCutOff + tRight * rightLegDirection));

            if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
            {
                line.direction = -obstacle1.direction;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                orcaLines.Add(line);
                continue;
            }

            if (distSqLeft <= distSqRight)
            {
                if (isLeftLegForeign) continue;
                line.direction = leftLegDirection;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                orcaLines.Add(line);
                continue;
            }

            if (isRightLegForeign) continue;
            line.direction = -rightLegDirection;
            line.point = rightCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
            orcaLines.Add(line);
        }

        int numObstLines = orcaLines.Count;
        int lineFail = linearProgram2(orcaLines, maxSpeed, prefVelocity, true, ref newVelocity);

        if (lineFail < orcaLines.Count)
        {
            linearProgram3(orcaLines, numObstLines, lineFail, maxSpeed, ref newVelocity);
        }

        velocity = newVelocity;
    }

    private int linearProgram2(List<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
    {
        if (directionOpt)
        {
            result = optVelocity * radius;
        }
        else if (RvoMath.AbsSq(optVelocity) > RvoMath.Sqr(radius))
        {
            result = RvoMath.normalize(optVelocity) * radius;
        }
        else
        {
            result = optVelocity;
        }

        for (int i = 0; i < lines.Count; ++i)
        {
            if (RvoMath.Det(lines[i].direction, lines[i].point - result) > 0.0f)
            {
                Vector2 tempResult = result;
                if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tempResult;
                    return i;
                }
            }
        }
        return lines.Count;
    }

    private bool linearProgram1(List<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
    {
        float dotProduct = RvoMath.Dot(lines[lineNo].point, lines[lineNo].direction);
        float discriminant = RvoMath.Sqr(dotProduct) + RvoMath.Sqr(radius) - RvoMath.AbsSq(lines[lineNo].point);

        if (discriminant < 0.0f) return false;

        float sqrtDiscriminant = RvoMath.Sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i)
        {
            float denominator = RvoMath.Det(lines[lineNo].direction, lines[i].direction);
            float numerator = RvoMath.Det(lines[i].direction, lines[lineNo].point - lines[i].point);

            if (RvoMath.fabs(denominator) <= RvoMath.RVO_EPSILON)
            {
                if (numerator < 0.0f) return false;
                continue;
            }

            float t = numerator / denominator;
            if (denominator >= 0.0f)
            {
                tRight = Mathf.Min(tRight, t);
            }
            else
            {
                tLeft = Mathf.Max(tLeft, t);
            }

            if (tLeft > tRight) return false;
        }

        if (directionOpt)
        {
            if (RvoMath.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
            {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            }
            else
            {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
        }
        else
        {
            float t = RvoMath.Dot(lines[lineNo].direction, (optVelocity - lines[lineNo].point));
            if (t < tLeft) result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            else if (t > tRight) result = lines[lineNo].point + tRight * lines[lineNo].direction;
            else result = lines[lineNo].point + t * lines[lineNo].direction;
        }

        return true;
    }

    private void linearProgram3(List<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result)
    {
        float distance = 0.0f;
        List<Line> projLines = new List<Line>();

        for (int i = beginLine; i < lines.Count; ++i)
        {
            if (RvoMath.Det(lines[i].direction, lines[i].point - result) > distance)
            {
                projLines.Clear();
                for (int ii = 0; ii < numObstLines; ++ii)
                {
                    projLines.Add(lines[ii]);
                }

                for (int j = numObstLines; j < i; ++j)
                {
                    Line line = new Line();
                    float determinant = RvoMath.Det(lines[i].direction, lines[j].direction);
                    if (RvoMath.fabs(determinant) <= RvoMath.RVO_EPSILON)
                    {
                        if (RvoMath.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                        {
                            continue;
                        }
                        else
                        {
                            line.point = new Vector2(
                                0.5f * (lines[i].point.x + lines[j].point.x),
                                0.5f * (lines[i].point.y + lines[j].point.y)
                            );
                        }
                    }
                    else
                    {
                        float t = RvoMath.Det(lines[j].direction, lines[i].point - lines[j].point) / determinant;
                        line.point = lines[i].point + t * lines[i].direction;
                    }
                    line.direction = RvoMath.normalize(lines[j].direction - lines[i].direction);
                    projLines.Add(line);
                }

                Vector2 tempResult = result;
                if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count)
                {
                    result = tempResult;
                }

                distance = RvoMath.Det(lines[i].direction, lines[i].point - result);
            }
        }
    }
}

public class Orca
{
    private static List<AABBObstacle> obstacles = new List<AABBObstacle>();

    public Orca()
    {
        obstacles.Clear();
    }

    public void AddObstacle(AABB_Rect aabb)
    {
        Obstacle o0 = new Obstacle { point = new Vector2(aabb.minX, aabb.minY) };
        Obstacle o1 = new Obstacle { point = new Vector2(aabb.maxX, aabb.minY) };
        Obstacle o2 = new Obstacle { point = new Vector2(aabb.maxX, aabb.maxY) };
        Obstacle o3 = new Obstacle { point = new Vector2(aabb.minX, aabb.maxY) };

        o0.next = o1; o0.previous = o3;
        o1.next = o2; o1.previous = o0;
        o2.next = o3; o2.previous = o1;
        o3.next = o0; o3.previous = o2;

        o0.direction = RvoMath.normalize(o1.point - o0.point);
        o1.direction = RvoMath.normalize(o2.point - o1.point);
        o2.direction = RvoMath.normalize(o3.point - o2.point);
        o3.direction = RvoMath.normalize(o0.point - o3.point);

        o0.convex = true;
        o1.convex = true;
        o2.convex = true;
        o3.convex = true;

        Obstacle[] result = { o0, o1, o2, o3 };
        AABBObstacle aabbObstacle = new AABBObstacle
        {
            minX = aabb.minX,
            minY = aabb.minY,
            maxX = aabb.maxX,
            maxY = aabb.maxY,
            obstacles = result
        };

        obstacles.Add(aabbObstacle);
    }

    public static List<Obstacle> QueryObstacles(Vector2 position, float radius)
    {
        List<Obstacle> result = new List<Obstacle>();
        float radiusSq = radius * radius;

        foreach (var aabb in obstacles)
        {
            float closestX = Mathf.Clamp(position.x, aabb.minX, aabb.maxX);
            float closestY = Mathf.Clamp(position.y, aabb.minY, aabb.maxY);
            float dx = position.x - closestX;
            float dy = position.y - closestY;

            if (dx * dx + dy * dy > radiusSq) continue;

            foreach (var obs in aabb.obstacles)
            {
                result.Add(obs);
            }
        }
        return result;
    }

    public void Execute(float timeHorizonObst, float timeHorizon, float timeStep, List<RvoAgent> agents)
    {
        foreach (RvoAgent agent in agents)
        {
            agent.Execute(timeHorizonObst, timeHorizon, timeStep, agents);
        }
    }
}