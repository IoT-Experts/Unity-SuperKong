﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if IAP
using UnityEngine.Purchasing;
#endif

[System.Serializable]
public struct ProductPurchase
{
	public string productNameApple;
	public string productNameGooglePlay;
	public UnityEvent eventPurchaseComplete;
}

public class IAPurchase : MonoBehaviour
#if IAP
, IStoreListener
#endif
{
	public List<ProductPurchase> ProductList;
	private List<string> ProductName;
	#if IAP
	private static IStoreController m_StoreController;                                                                  // Reference to the Purchasing system.
	private static IExtensionProvider m_StoreExtensionProvider;                                                         // Reference to store-specific Purchasing subsystems.

	void Start()
	{
		// If we haven't set up the Unity Purchasing reference
		if (m_StoreController == null)
		{
			// Begin to configure our connection to Purchasing
			InitializePurchasing();
		}
	}

	void InitializePurchasing() 
	{
		// If we have already connected to Purchasing ...
		if (IsInitialized())
		{
			// ... we are done here.
			return;
		}

		// Create a builder, first passing in a suite of Unity provided stores.
		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

		ProductName = new List<string>();
		for(int i = 0; i < ProductList.Count; i++)
		{
			ProductName.Add ("pd" + i.ToString ());
			builder.AddProduct(ProductName[i], ProductType.Consumable, new IDs(){{ ProductList[i].productNameApple,       AppleAppStore.Name },{ ProductList[i].productNameGooglePlay,  GooglePlay.Name },});
		}
		UnityPurchasing.Initialize(this, builder);
	}
		
	private bool IsInitialized()
	{
		// Only say we are initialized if both the Purchasing references are set.
		return m_StoreController != null && m_StoreExtensionProvider != null;
	}

	void BuyProductID(string productId)
	{
		foreach(var item in m_StoreController.products.all)
		{
			print (m_StoreController.products.all.Length);
		}
		// If the stores throw an unexpected exception, use try..catch to protect my logic here.
		try
		{
			// If Purchasing has been initialized ...
			if (IsInitialized())
			{
				// ... look up the Product reference with the general product identifier and the Purchasing system's products collection.
				Product product = m_StoreController.products.WithID(productId);

				// If the look up found a product for this device's store and that product is ready to be sold ... 
				if (product != null && product.availableToPurchase)
				{
					Debug.Log (string.Format("Purchasing product asychronously: '{0}'", product.definition.id));// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously.
					m_StoreController.InitiatePurchase(product);
				}
				// Otherwise ...
				else
				{
					// ... report the product look-up failure situation  
					Debug.Log ("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
				}
			}
			// Otherwise ...
			else
			{
				// ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or retrying initiailization.
				Debug.Log("BuyProductID FAIL. Not initialized.");
			}
		}
		// Complete the unexpected exception handling ...
		catch (Exception e)
		{
			// ... by reporting any unexpected exception for later diagnosis.
			Debug.Log ("BuyProductID: FAIL. Exception during purchase. " + e);
		}
	}

	//  
	// --- IStoreListener
	//

	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
	// Purchasing has succeeded initializing. Collect our Purchasing references.
	Debug.Log("OnInitialized: PASS");

	// Overall Purchasing system, configured with products for this application.
	m_StoreController = controller;
	// Store specific subsystem, for accessing device-specific store features.
	m_StoreExtensionProvider = extensions;
	}


	public void OnInitializeFailed(InitializationFailureReason error)
	{
	// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
	Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
	}


	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) 
	{
	for(int i = 0; i < ProductList.Count; i ++)
	{
	if (String.Equals(args.purchasedProduct.definition.id, ProductName[i], StringComparison.Ordinal))
	{
	//Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));//If the consumable item has been successfully purchased, add 100 coins to the player's in-game score.
	ProductList[i].eventPurchaseComplete.Invoke ();
	}
	else 
	{
	Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
	}// Return a flag indicating wither this product has completely been received, or if the application needs to be reminded of this purchase at next app launch. Is useful when saving purchased products to the cloud, and when that save is delayed.
	}
	return PurchaseProcessingResult.Complete;
	}


	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
	// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing this reason with the user.
	Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}",product.definition.storeSpecificId, failureReason));
	}
	#endif

	public void BuyProduct(int indexProduct)
	{
		#if IAP
		BuyProductID(ProductName[indexProduct]);
		#endif
	}

	// Restore purchases previously made by this customer. Some platforms automatically restore purchases. Apple currently requires explicit purchase restoration for IAP.
	public void RestorePurchases()
	{
		#if IAP
		// If Purchasing has not yet been set up ...
		if (!IsInitialized())
		{
			// ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
			Debug.Log("RestorePurchases FAIL. Not initialized.");
			return;
		}

		// If we are running on an Apple device ... 
		if (Application.platform == RuntimePlatform.IPhonePlayer || 
			Application.platform == RuntimePlatform.OSXPlayer)
		{
			// ... begin restoring purchases
			Debug.Log("RestorePurchases started ...");

			// Fetch the Apple store-specific subsystem.
			var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
			// Begin the asynchronous process of restoring purchases. Expect a confirmation response in the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
			apple.RestoreTransactions((result) => {
				// The first phase of restoration. If no more responses are received on ProcessPurchase then no purchases are available to be restored.
				Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
			});
		}
		// Otherwise ...
		else
		{
			// We are not running on an Apple device. No work is necessary to restore purchases.
			Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
		}
		#endif
	}
		
}
