using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>A fridge together with one user's membership of it and how many members it has.</summary>
public record FridgeWithMembership(Fridge Fridge, FridgeMember Member, int MemberCount);
