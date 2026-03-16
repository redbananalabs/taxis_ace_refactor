# Red Taxi Platform — SaaS Packaging & Pricing

**Version:** 2.0  
**Last Updated:** 2026-03-16

---

## 1. Why Packaging Matters

This SaaS serves operators of very different sizes. A small rural operator with ~20 drivers and ~100 daily bookings has completely different needs from a larger regional fleet with 100+ drivers and partner networks. Pricing must reflect this.

---

## 2. Entitlement Dimensions

| Dimension | Description |
|-----------|-------------|
| Active drivers | Max drivers who can be dispatched |
| Monthly bookings | Booking volume cap (soft or hard) |
| Operator seats | Number of dispatch console users |
| Vehicles | Number of vehicle records |
| Partner network | Access to cross-company job sharing |
| API access | Public booking API, webhooks |
| Advanced analytics | KPI dashboards, trend reports |
| AI automation | Chatbot, auto-dispatch, voice booking |
| White-label websites | SEO booking sites per tenant |

---

## 3. Recommended Tiers

### Starter
For smaller owner-managed fleets.

| Entitlement | Limit |
|-------------|-------|
| Active drivers | Up to 15 |
| Monthly bookings | Up to 3,000 |
| Operator seats | 2 |
| Vehicles | 15 |
| Core booking + dispatch | Yes |
| Basic reporting | Yes |
| SMS notifications | Yes (usage-billed) |
| Driver app | Yes |
| Partner network | No |
| API access | No |
| White-label website | No |
| AI features | No |

### Growth
For established local operators.

| Entitlement | Limit |
|-------------|-------|
| Active drivers | Up to 40 |
| Monthly bookings | Up to 10,000 |
| Operator seats | 5 |
| Vehicles | 40 |
| All Starter features | Yes |
| Pricing rules & tariffs | Yes |
| Account management | Yes |
| WhatsApp notifications | Yes |
| Payment links | Yes |
| Advanced reporting | Yes |
| API access | Yes |
| Partner network | No |
| White-label website | 1 |
| AI features | No |

### Professional
For multi-operator or regional businesses.

| Entitlement | Limit |
|-------------|-------|
| Active drivers | Up to 100 |
| Monthly bookings | Up to 30,000 |
| Operator seats | 15 |
| Vehicles | 100 |
| All Growth features | Yes |
| Partner network | Yes (up to 5 partners) |
| Settlement automation | Yes |
| White-label websites | Up to 5 |
| WhatsApp chatbot | Yes |
| Priority support | Yes |
| AI dispatch suggestions | Yes |

### Enterprise
For larger networks or bespoke needs.

| Entitlement | Limit |
|-------------|-------|
| Active drivers | Custom |
| Monthly bookings | Custom |
| Operator seats | Custom |
| All Professional features | Yes |
| Unlimited partners | Yes |
| Custom onboarding | Yes |
| Dedicated support | Yes |
| AI voice booking | Yes |
| Auto-dispatch | Yes |
| Custom integrations | Yes |
| SLA guarantee | Yes |

---

## 4. Usage-Based Billing (Add-ons)

| Item | Billing Model |
|------|--------------|
| SMS messages | Per message (Twilio pass-through + margin) |
| WhatsApp messages | Per conversation (WhatsApp Business pricing + margin) |
| Address lookups | Per lookup (Ideal Postcodes pass-through + margin) |
| Extra booking volume | Per 1,000 bookings above tier limit |
| Extra operator seats | Per seat per month |
| Extra drivers | Per driver per month above tier limit |
| AI voice minutes | Per minute |

---

## 5. Internal Implementation Requirements

The platform must support:

| Requirement | Description |
|-------------|-------------|
| Plan assignment | Each tenant has a plan (tier) |
| Usage counters | Track bookings, drivers, seats, API calls per billing period |
| Soft limits | Warn when approaching limit (e.g. 80% of booking quota) |
| Hard limits | Block at limit (e.g. can't add more drivers than tier allows) |
| Feature flags | Gate features by tier (partner network, AI, API, etc.) |
| Upgrade prompts | Show upgrade CTAs when hitting limits |
| Billing integration | Stripe / payment provider for subscription + usage billing |
| Trial period | Free trial with Starter features for X days |

---

## 6. Pricing Strategy Notes

- **Don't undercharge small operators** — even Starter needs to cover support costs
- **Don't overcharge for growth** — operators should feel rewarded for growing on the platform
- **Usage-based add-ons** create upsell without forcing tier upgrades
- **Partner network** is a natural Professional+ feature — it creates network effects
- **AI features** are premium differentiators for Professional/Enterprise
- **White-label websites** are high-value and sticky — they generate leads that lock operators into the platform

---

## 7. Alternative Pricing Models

Beyond the tiered model, the platform should support:

### Per-Booking Pricing
Small fee per completed booking (£0.30–£1.00). Useful for operators with fluctuating demand who don't want fixed monthly costs.

### Hybrid Pricing
Base subscription + per-booking fee. Example: £99/month + £0.50 per booking. Combines predictable revenue with usage-based scaling.

### Per-Driver Pricing
Monthly fee based on active driver count:
- Up to 10 drivers: £99/month
- Up to 50 drivers: £299/month
- Unlimited: £599/month

### Recommendation
Start with tiered (Starter/Growth/Professional/Enterprise) for v1 SaaS launch. Add per-booking and hybrid models in Phase 5+ once usage patterns are understood from real tenants.
