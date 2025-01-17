---
uid: addressables-for-android-edit-groups
---

# Edit Addressable groups for Play Asset Delivery

To make Addressable groups use Play Asset Delivery functionality, you must add [Play Asset Delivery schema](play-asset-delivery-schema-reference.md) to the groups. Then, either edit the **Delivery Type** property or select **Custom Asset Pack**.

Also, make sure that [Content Packing & Loading schema](xref:addressables-content-packing-and-loading-schema) is present.

A new Addressable group can use the **Play Asset Delivery Content template** or [a template created from scratch](xref:group-templates). Play Asset Delivery Content template is the Addressable group template which you can use to create a new group with Play Asset Delivery support. This template includes two schemas: [Content Packing & Loading schema](xref:addressables-content-packing-and-loading-schema) and [Play Asset Delivery schema](play-asset-delivery-schema-reference.md).

If the new Addressable group uses a template other than the Play Asset Delivery Content template, you can still add the Content Packing & Loading schema and Play Asset Delivery schema to the group. It's important to add both the schemas to the Addressable group for Play Asset Delivery support.

Working with groups which use Play Asset Delivery functionality on Android is no different from working with groups which don't need this functionality. If some groups exceed [Google limit for Play Asset Delivery](https://developer.android.com/guide/playcore/asset-delivery#download-size-limits) due to their large size, you can apply a combination of Play Asset Delivery to some groups and [remote hosting](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/remote-content-enable.html) to others. The groups using remote hosting should not include Play Asset Delivery schema.

## Additional resources
* [Addressable Groups](xref:addressables-groups).
